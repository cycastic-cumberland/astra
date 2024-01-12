using System.Data;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Astra.Client.Aggregator;
using Astra.Common;
using Microsoft.IO;

namespace Astra.Client;

// Side job: handle endianness 
public class SimpleAstraClient : IAstraClient
{
    public class EndianModeNotSupportedException(string? msg = null) : NotSupportedException(msg);
    public class HandshakeFailedException(string? msg = null) : Exception(msg);
    public class VersionNotSupportedException(string? msg = null) : NotSupportedException(msg);
    public class AuthenticationMethodNotSupportedException(string? msg = null) : Exception(msg);
    public class AuthenticationInfoNotProvidedException(string? msg = null) : Exception(msg);
    public class AuthenticationAttemptRejectedException(string? msg = null) : Exception(msg);
    public class NotConnectedException(string? msg = null) : Exception(msg);
    public class FaultedRequestException(string? msg = null) : Exception(msg);
    private readonly struct InternalClient(TcpClient client, NetworkStream clientStream, bool reversedOrder) : IDisposable
    {
        public TcpClient Client
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => client;
        }
        public NetworkStream ClientStream
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => clientStream;
        }

        public bool ShouldReverse
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => reversedOrder;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Deconstruct(out TcpClient outClient, out NetworkStream outClientStream, out bool reversed)
        {
            outClient = client;
            outClientStream = clientStream;
            reversed = reversedOrder;
        }

        public readonly bool IsConnected = true;
        public void Dispose()
        {
            client.Close();
        }
    }

    private const long HeaderSize = sizeof(uint) + sizeof(uint); // commandCount + commandType
    private const long InsertHeaderSize = HeaderSize + sizeof(int); // commandCount + commandType + rowCount

    private readonly RecyclableMemoryStream _inStream = MemoryStreamPool.Allocate();
    private readonly BytesClusterStream _shortOutStream = BytesClusterStream.Rent(16);
    private InternalClient? _client;

    public void Dispose()
    {
        _client?.Dispose();
        _shortOutStream.Dispose();
        _inStream.Dispose();
    }

    public AstraConnectionSettings? ConnectionSettings { get; private set; }
    public bool IsConnected => _client?.IsConnected ?? false;

    public async Task ConnectAsync(AstraConnectionSettings settings, CancellationToken cancellationToken = default)
    {
        _client?.Dispose();
        _client = null;
        if (settings.Timeout == 0) settings.Timeout = 100_000; // 100 seconds
        ConnectionSettings = settings;
        var networkClient = new TcpClient(settings.Address, settings.Port);
        var networkStream = networkClient.GetStream();
        try
        {
            var timer = Stopwatch.StartNew();
            // Wait for endian check and handshake signal
            while (networkClient.Available < sizeof(int) + sizeof(ulong))
            {
#if DEBUG
                await Task.Delay(100, cancellationToken);
#endif
                if (timer.ElapsedMilliseconds <= settings.Timeout) continue;
                throw new TimeoutException($"Timed out: {settings.Timeout} ms");
            }
            using var outBuffer = BytesCluster.Rent(sizeof(ulong));
            _ = await networkStream.ReadAsync(outBuffer.WriterMemory[..sizeof(int)], cancellationToken);
            var isLittleEndian = outBuffer.Reader[0] == 1;
            if (isLittleEndian != BitConverter.IsLittleEndian)
                throw new EndianModeNotSupportedException($"Endianness not supported: {(isLittleEndian ? "little endian" : "big endian")}");
            await networkStream.ReadExactlyAsync(outBuffer.WriterMemory[..sizeof(ulong)], cancellationToken);
            var handshakeSignal = BitConverter.ToUInt64(outBuffer.Reader[..sizeof(ulong)]);
            if (handshakeSignal != CommunicationProtocol.ServerIdentification)
            {
                throw new HandshakeFailedException();
            }

            await networkStream.WriteValueAsync(CommunicationProtocol.HandshakeResponse, cancellationToken);
            timer.Restart();
            while (networkClient.Available < sizeof(uint))
            {
#if DEBUG
                await Task.Delay(100, cancellationToken);
#endif
                if (timer.ElapsedMilliseconds <= settings.Timeout) continue;
                throw new TimeoutException($"Timed out: {settings.Timeout} ms");
            }
            await networkStream.ReadExactlyAsync(outBuffer.WriterMemory[..sizeof(uint)], cancellationToken);
            var serverVersion = BitConverter.ToUInt32(outBuffer.Reader[..sizeof(uint)]);
            if (serverVersion != CommonProtocol.AstraCommonVersion) throw new VersionNotFoundException($"Astra.Server version not supported: {serverVersion}");
            timer.Restart();
            while (networkClient.Available < sizeof(uint))
            {
#if DEBUG
                await Task.Delay(100, cancellationToken);
#endif
                if (timer.ElapsedMilliseconds <= settings.Timeout) continue;
                throw new TimeoutException($"Timed out: {settings.Timeout} ms");
            }
            await networkStream.ReadExactlyAsync(outBuffer.WriterMemory[..sizeof(uint)], cancellationToken);
            var authMethod = BitConverter.ToUInt32(outBuffer.Reader[..sizeof(uint)]);
            switch (authMethod)
            {
                case CommunicationProtocol.NoAuthentication:
                {
                    break;
                }
                case CommunicationProtocol.PasswordAuthentication:
                {
                    if (string.IsNullOrEmpty(settings.Password))
                        throw new AuthenticationInfoNotProvidedException(nameof(settings.Password));
                    var pwdBytes = Encoding.UTF8.GetBytes(settings.Password);
                    await networkStream.WriteValueAsync(pwdBytes.Length, token: cancellationToken);
                    await networkStream.WriteAsync(pwdBytes, cancellationToken);
                    break;
                }
                case CommunicationProtocol.PubKeyAuthentication:
                {
                    using RSA rsa = new RSACryptoServiceProvider();
                    if (string.IsNullOrEmpty(settings.PrivateKey))
                        throw new AuthenticationInfoNotProvidedException(nameof(settings.PrivateKey));
                    rsa.ImportRSAPrivateKey(Convert.FromBase64String(settings.PrivateKey), out _);
                    var dataBytes = BitConverter.GetBytes(CommunicationProtocol.PubKeyPayload);
                    var signatureBytes = rsa.SignData(new ReadOnlySpan<byte>(dataBytes), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                    await networkStream.WriteValueAsync(signatureBytes.LongLength, token: cancellationToken);
                    await networkStream.WriteAsync(signatureBytes, cancellationToken);
                    break;
                }
                default:
                    throw new AuthenticationMethodNotSupportedException($"Authentication method not supported: {authMethod}");
            }
            timer.Restart();
            while (networkClient.Available < sizeof(uint))
            {
#if DEBUG
                await Task.Delay(100, cancellationToken);
#endif
                if (timer.ElapsedMilliseconds <= settings.Timeout) continue;
                throw new TimeoutException($"Timed out: {settings.Timeout} ms");
            }

            await networkStream.ReadExactlyAsync(outBuffer.WriterMemory[..sizeof(uint)], cancellationToken);
            var attemptResult = BitConverter.ToUInt32(outBuffer.Reader[..sizeof(uint)]);
            if (attemptResult != CommunicationProtocol.AllowedConnection)
                throw new AuthenticationAttemptRejectedException();
            _client = new(networkClient, networkStream, false);
        }
        catch (Exception)
        {
            networkClient.Dispose();
            throw;
        }
    }
    public async Task<int> InsertSerializableAsync<T>(T value, CancellationToken cancellationToken = default) where T : IAstraSerializable
    {
        var (client, clientStream, reversed) = _client ?? throw new NotConnectedException();
        _inStream.Position = 0;
        if (reversed)
            value.SerializeStream(new ReverseStreamWrapper(_inStream));
        else value.SerializeStream(new ForwardStreamWrapper(_inStream));
        await clientStream.WriteValueAsync(InsertHeaderSize + _inStream.Position, cancellationToken);
        await clientStream.WriteValueAsync(Command.CreateWriteHeader(1U), cancellationToken); // 1 command
        await clientStream.WriteValueAsync(Command.UnsortedInsert, cancellationToken); // Command type (insert)
        await clientStream.WriteValueAsync(1, cancellationToken); // 1 row
        await clientStream.WriteAsync(new ReadOnlyMemory<byte>(_inStream.GetBuffer(),
            0, (int)_inStream.Position), cancellationToken);
        var timer = Stopwatch.StartNew();
        while (client.Available < sizeof(long))
        {
#if DEBUG
                await Task.Delay(100, cancellationToken);
#endif
            if (timer.ElapsedMilliseconds > ConnectionSettings!.Value.Timeout)
                throw new TimeoutException();
        }

        var outStreamSize = await clientStream.ReadLongAsync(cancellationToken);
        timer.Restart();
        while (client.Available < outStreamSize)
        {
#if DEBUG
                await Task.Delay(100, cancellationToken);
#endif
            if (timer.ElapsedMilliseconds > ConnectionSettings!.Value.Timeout)
                throw new TimeoutException();
        }
        _ = await clientStream.ReadAsync(_shortOutStream.AsMemory()[..(int)outStreamSize], cancellationToken);
        _shortOutStream.Position = 0;
        var faulted = (byte)_shortOutStream.ReadByte();
        if (faulted != 0) throw new FaultedRequestException();
        var inserted = _shortOutStream.ReadInt();
        return inserted;
    }

    public async Task<int> BulkInsertSerializableAsync<T>(IEnumerable<T> values, CancellationToken cancellationToken = default) where T : IAstraSerializable
    {
        var (client, clientStream, reversed) = _client ?? throw new NotConnectedException();
        _inStream.Position = 0;
        var count = 0;
        if (reversed)
        {
            var wrapper = new ReverseStreamWrapper(_inStream);
            foreach (var value in values)
            {
                value.SerializeStream(wrapper);
                count++;
            }
        }
        else
        {
            var wrapper = new ForwardStreamWrapper(_inStream);
            foreach (var value in values)
            {
                value.SerializeStream(wrapper);
                count++;
            }
        }

        await clientStream.WriteValueAsync(InsertHeaderSize + _inStream.Position, cancellationToken);
        await clientStream.WriteValueAsync(Command.CreateWriteHeader(1U), cancellationToken); // 1 command
        await clientStream.WriteValueAsync(Command.UnsortedInsert, cancellationToken); // Command type (insert)
        await clientStream.WriteValueAsync(count, cancellationToken); // `count` rows
        await clientStream.WriteAsync(new ReadOnlyMemory<byte>(_inStream.GetBuffer(),
            0, (int)_inStream.Position), cancellationToken);
        var timer = Stopwatch.StartNew();
        while (client.Available < sizeof(long))
        {
#if DEBUG
                await Task.Delay(100, cancellationToken);
#endif
            if (timer.ElapsedMilliseconds > ConnectionSettings!.Value.Timeout)
                throw new TimeoutException();
        }

        var outStreamSize = await clientStream.ReadLongAsync(cancellationToken);
        timer.Restart();
        while (client.Available < outStreamSize)
        {
#if DEBUG
                await Task.Delay(100, cancellationToken);
#endif
            if (timer.ElapsedMilliseconds > ConnectionSettings!.Value.Timeout)
                throw new TimeoutException();
        }
        _ = await clientStream.ReadAsync(_shortOutStream.AsMemory()[..(int)outStreamSize], cancellationToken);
        _shortOutStream.Position = 0;
        var faulted = (byte)_shortOutStream.ReadByte();
        if (faulted != 0) throw new FaultedRequestException();
        var inserted = _shortOutStream.ReadInt();
        return inserted;
    }
    private async Task<IEnumerable<T>> AggregateInternalAsync<T>(ReadOnlyMemory<byte> predicateStream, CancellationToken cancellationToken = default) where T : IAstraSerializable
    {
        var (client, clientStream, reversed) = _client ?? throw new NotConnectedException();
        await clientStream.WriteValueAsync(HeaderSize + predicateStream.Length, cancellationToken);
        await clientStream.WriteValueAsync(Command.CreateReadHeader(1U), cancellationToken); // 1 command
        await clientStream.WriteValueAsync(Command.UnsortedAggregate, cancellationToken); // Command type (aggregate)
        await clientStream.WriteAsync(predicateStream, cancellationToken);
        var timer = Stopwatch.StartNew();
        while (client.Available < sizeof(long))
        {
#if DEBUG
                await Task.Delay(100, cancellationToken);
#endif
            if (timer.ElapsedMilliseconds > ConnectionSettings!.Value.Timeout)
                throw new TimeoutException();
        }

        var outStreamSize = await clientStream.ReadLongAsync(cancellationToken);
        timer.Restart();
        while (client.Available < outStreamSize)
        {
#if DEBUG
                await Task.Delay(100, cancellationToken);
#endif
            if (timer.ElapsedMilliseconds > ConnectionSettings!.Value.Timeout)
                throw new TimeoutException();
        }

        var outCluster = BytesCluster.Rent((int)outStreamSize);
        await clientStream.ReadExactlyAsync(outCluster.WriterMemory, cancellationToken);
        var stream = outCluster.Promote();
        var faulted = (byte)stream.ReadByte();
        if (faulted != 0) throw new FaultedRequestException();
        return IAstraSerializable.DeserializeStream<T, BytesClusterStream>(stream, reversed);
    }

    public Task<IEnumerable<T>> AggregateAsync<T>(IAstraQueryBranch predicate, CancellationToken cancellationToken = default) where T : IAstraSerializable
    {
        return AggregateInternalAsync<T>(predicate.DumpMemory(), cancellationToken);
    }

    public async Task<int> CountAllAsync(CancellationToken cancellationToken = default)
    {
        var (client, clientStream, _) = _client ?? throw new NotConnectedException();
        await clientStream.WriteValueAsync(HeaderSize, cancellationToken);
        await clientStream.WriteValueAsync(Command.CreateReadHeader(1U), cancellationToken); // 1 command
        await clientStream.WriteValueAsync(Command.CountAll, cancellationToken); // Command type (count all)
        var timer = Stopwatch.StartNew();
        while (client.Available < sizeof(long))
        {
#if DEBUG
                await Task.Delay(100, cancellationToken);
#endif
            if (timer.ElapsedMilliseconds > ConnectionSettings!.Value.Timeout)
                throw new TimeoutException();
        }

        var outStreamSize = await clientStream.ReadLongAsync(cancellationToken);
        timer.Restart();
        while (client.Available < outStreamSize)
        {
#if DEBUG
                await Task.Delay(100, cancellationToken);
#endif
            if (timer.ElapsedMilliseconds > ConnectionSettings!.Value.Timeout)
                throw new TimeoutException();
        }
        var outCluster = BytesCluster.Rent((int)outStreamSize);
        await clientStream.ReadExactlyAsync(outCluster.WriterMemory, cancellationToken);
        using var stream = outCluster.Promote();
        var faulted = (byte)stream.ReadByte();
        if (faulted != 0) throw new FaultedRequestException();
        return stream.ReadInt();
    }

    private async Task<int> ConditionalCountInternalAsync(ReadOnlyMemory<byte> predicateStream, CancellationToken cancellationToken = default)
    {
        var (client, clientStream, _) = _client ?? throw new NotConnectedException();
        await clientStream.WriteValueAsync(HeaderSize + predicateStream.Length, cancellationToken);
        await clientStream.WriteValueAsync(Command.CreateReadHeader(1U), cancellationToken); // 1 command
        await clientStream.WriteValueAsync(Command.ConditionalCount, cancellationToken); // Command type (conditional count)
        await clientStream.WriteAsync(predicateStream, cancellationToken);
        var timer = Stopwatch.StartNew();
        while (client.Available < sizeof(long))
        {
#if DEBUG
                await Task.Delay(100, cancellationToken);
#endif
            if (timer.ElapsedMilliseconds > ConnectionSettings!.Value.Timeout)
                throw new TimeoutException();
        }

        var outStreamSize = await clientStream.ReadLongAsync(cancellationToken);
        timer.Restart();
        while (client.Available < outStreamSize)
        {
#if DEBUG
                await Task.Delay(100, cancellationToken);
#endif
            if (timer.ElapsedMilliseconds > ConnectionSettings!.Value.Timeout)
                throw new TimeoutException();
        }

        var outCluster = BytesCluster.Rent((int)outStreamSize);
        await clientStream.ReadExactlyAsync(outCluster.WriterMemory, cancellationToken);
        using var stream = outCluster.Promote();
        var faulted = (byte)stream.ReadByte();
        if (faulted != 0) throw new FaultedRequestException();
        return stream.ReadInt();
    }
    
    public Task<int> ConditionalCountAsync<TA>(TA predicate, CancellationToken cancellationToken = default) where TA : IAstraQueryBranch
    {
        return ConditionalCountInternalAsync(predicate.DumpMemory(), cancellationToken);
    }
    
    private async Task<int> ConditionalDeleteInternalAsync(ReadOnlyMemory<byte> predicateStream, CancellationToken cancellationToken = default) 
    {
        var (client, clientStream, _) = _client ?? throw new NotConnectedException();
        await clientStream.WriteValueAsync(HeaderSize + predicateStream.Length, cancellationToken);
        await clientStream.WriteValueAsync(Command.CreateWriteHeader(1U), cancellationToken); // 1 command
        await clientStream.WriteValueAsync(Command.ConditionalDelete, cancellationToken); // Command type (conditional count)
        await clientStream.WriteAsync(predicateStream, cancellationToken);
        var timer = Stopwatch.StartNew();
        while (client.Available < sizeof(long))
        {
#if DEBUG
                await Task.Delay(100, cancellationToken);
#endif
            if (timer.ElapsedMilliseconds > ConnectionSettings!.Value.Timeout)
                throw new TimeoutException();
        }

        var outStreamSize = await clientStream.ReadLongAsync(cancellationToken);
        timer.Restart();
        while (client.Available < outStreamSize)
        {
#if DEBUG
                await Task.Delay(100, cancellationToken);
#endif
            if (timer.ElapsedMilliseconds > ConnectionSettings!.Value.Timeout)
                throw new TimeoutException();
        }

        var outCluster = BytesCluster.Rent((int)outStreamSize);
        await clientStream.ReadExactlyAsync(outCluster.WriterMemory, cancellationToken);
        using var stream = outCluster.Promote();
        var faulted = (byte)stream.ReadByte();
        if (faulted != 0) throw new FaultedRequestException();
        return stream.ReadInt();
    }

    public Task<int> ConditionalDeleteAsync<TA>(TA predicate, CancellationToken cancellationToken = default) where TA : IAstraQueryBranch
    {
        return ConditionalDeleteInternalAsync(predicate.DumpMemory(), cancellationToken);
    }

    public async Task<int> ClearAsync(CancellationToken cancellationToken = default)
    {
        var (client, clientStream, _) = _client ?? throw new NotConnectedException();
        await clientStream.WriteValueAsync(HeaderSize, cancellationToken);
        await clientStream.WriteValueAsync(Command.CreateWriteHeader(1U), cancellationToken); // 1 command
        await clientStream.WriteValueAsync(Command.Clear, cancellationToken); // Command type (count all)
        var timer = Stopwatch.StartNew();
        while (client.Available < sizeof(long))
        {
#if DEBUG
                await Task.Delay(100, cancellationToken);
#endif
            if (timer.ElapsedMilliseconds > ConnectionSettings!.Value.Timeout)
                throw new TimeoutException();
        }

        var outStreamSize = await clientStream.ReadLongAsync(cancellationToken);
        timer.Restart();
        while (client.Available < outStreamSize)
        {
#if DEBUG
                await Task.Delay(100, cancellationToken);
#endif
            if (timer.ElapsedMilliseconds > ConnectionSettings!.Value.Timeout)
                throw new TimeoutException();
        }
        var outCluster = BytesCluster.Rent((int)outStreamSize);
        await clientStream.ReadExactlyAsync(outCluster.WriterMemory, cancellationToken);
        using var stream = outCluster.Promote();
        var faulted = (byte)stream.ReadByte();
        if (faulted != 0) throw new FaultedRequestException();
        return stream.ReadInt();
    }
}