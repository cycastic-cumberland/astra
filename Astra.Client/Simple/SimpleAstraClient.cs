using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Astra.Client.Entity;
using Astra.Client.Simple.Aggregator;
using Astra.Common.Data;
using Astra.Common.Hashes;
using Astra.Common.Protocols;
using Astra.Common.Serializable;
using Astra.Common.StreamUtils;
using Microsoft.IO;

namespace Astra.Client.Simple;


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
    public class ConcurrencyException(string? msg = null) : Exception(msg);
    internal readonly struct InternalClient(TcpClient client, NetworkStream clientStream, bool reversedOrder) : IDisposable
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
    
    internal readonly struct ExclusivityCheck : IDisposable
    {
        private readonly SimpleAstraClient _client;
        public ExclusivityCheck(SimpleAstraClient client)
        {
            if (client._exclusivity)
                throw new ConcurrencyException("Multiple readers cannot exist at the same time");
            client._exclusivity = true;
            _client = client;
        }

        public void Dispose()
        {
            _client._exclusivity = false;
        }
    }

    private const long HeaderSize = sizeof(uint) + sizeof(uint); // commandCount + commandType
    private const long InsertHeaderSize = HeaderSize + sizeof(int); // commandCount + commandType + rowCount

    private readonly RecyclableMemoryStream _inStream = MemoryStreamPool.Allocate();
    private readonly BytesClusterStream _shortOutStream = BytesClusterStream.Rent(16);
    private InternalClient? _client;
    private bool _exclusivity;

    internal InternalClient? Client => _client;
    
    public void Dispose()
    {
        _client?.Dispose();
        _shortOutStream.Dispose();
        _inStream.Dispose();
    }

    public SimpleAstraClientConnectionSettings? ConnectionSettings { get; private set; }
    public bool IsConnected => _client?.IsConnected ?? false;

    public async Task ConnectAsync(SimpleAstraClientConnectionSettings settings, CancellationToken cancellationToken = default)
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
            while (networkClient.Available < sizeof(int) + sizeof(ulong) + sizeof(uint))
            {
#if DEBUG
                await Task.Delay(100, cancellationToken);
#else
                Thread.Yield();
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
            await networkStream.ReadExactlyAsync(outBuffer.WriterMemory[..sizeof(uint)], cancellationToken);
            var serverVersion = BitConverter.ToUInt32(outBuffer.Reader[..sizeof(uint)]);
            if (serverVersion != CommonProtocol.AstraCommonVersion) 
                throw new VersionNotSupportedException($"Astra.Server version not supported: {serverVersion.ToAstraCommonVersion()}");

            await networkStream.WriteValueAsync(CommunicationProtocol.SimpleClientResponse, cancellationToken);
            timer.Restart();
            while (networkClient.Available < sizeof(uint))
            {
#if DEBUG
                await Task.Delay(100, cancellationToken);
#else
                Thread.Yield();
#endif
                if (timer.ElapsedMilliseconds <= settings.Timeout) continue;
                throw new TimeoutException($"Timed out: {settings.Timeout} ms");
            }
            timer.Restart();
            while (networkClient.Available < sizeof(uint))
            {
#if DEBUG
                await Task.Delay(100, cancellationToken);
#else
                Thread.Yield();
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
                case CommunicationProtocol.SaltedPasswordAuthentication:
                {
                    if (string.IsNullOrEmpty(settings.Password))
                        throw new AuthenticationInfoNotProvidedException(nameof(settings.Password));
                    using var salt = BytesCluster.Rent(CommonProtocol.SaltLength);
                    while (networkClient.Available < CommonProtocol.SaltLength)
                    {
#if DEBUG
                        await Task.Delay(100, cancellationToken);
#else
                        Thread.Yield();
#endif
                        if (timer.ElapsedMilliseconds <= settings.Timeout) continue;
                        throw new TimeoutException($"Timed out: {settings.Timeout} ms");
                    }

                    await networkStream.ReadExactlyAsync(salt.WriterMemory, cancellationToken);
                    using var passwordBytes = BytesCluster.Rent(settings.Password.Length * 4);
                    var passwordBytesLength = Encoding.UTF8.GetBytes(settings.Password, passwordBytes.Writer);
                    using var combined = CommonProtocol.CombineSalt(passwordBytes.Reader[..passwordBytesLength], salt.Reader);
                    var hashed = Hash256.HashSha256(combined.Reader);
                    await networkStream.WriteValueAsync(hashed, cancellationToken);
                    break;
                }
                case CommunicationProtocol.PubKeyAuthentication:
                {
                    while (networkClient.Available < sizeof(long))
                    {
#if DEBUG
                        await Task.Delay(100, cancellationToken);
#else
                        Thread.Yield();
#endif
                        if (timer.ElapsedMilliseconds <= settings.Timeout) continue;
                        throw new TimeoutException($"Timed out: {settings.Timeout} ms");
                    }
                    await networkStream.ReadExactlyAsync(outBuffer.WriterMemory[..sizeof(long)], cancellationToken);
                    var challengeLength = BitConverter.ToUInt32(outBuffer.Reader[..sizeof(long)]);
                    using var challenge = BytesCluster.Rent(challengeLength);
                    while (networkClient.Available < challengeLength)
                    {
#if DEBUG
                        await Task.Delay(100, cancellationToken);
#else
                        Thread.Yield();
#endif
                        if (timer.ElapsedMilliseconds <= settings.Timeout) continue;
                        throw new TimeoutException($"Timed out: {  settings.Timeout} ms");
                    }

                    await networkStream.ReadExactlyAsync(challenge.WriterMemory, cancellationToken);
                    using RSA rsa = new RSACryptoServiceProvider(CommonProtocol.SignatureSizeBit);
                    if (string.IsNullOrEmpty(settings.PrivateKey))
                        throw new AuthenticationInfoNotProvidedException(nameof(settings.PrivateKey));
                    using (var privateKeyBytes = BytesCluster.Rent(settings.PrivateKey.Length * 3 / 4))
                    {
                        var converted = Convert.TryFromBase64String(settings.PrivateKey, privateKeyBytes.Writer, out var bytesWritten);
                        if (!converted) throw new FormatException(nameof(settings.PrivateKey));
                        rsa.ImportRSAPrivateKey(privateKeyBytes.Reader[..bytesWritten], out _);
                    }

                    using var signatureBytes = BytesCluster.Rent(CommonProtocol.SignatureSize);
                    var written = rsa.SignData(challenge.Reader, signatureBytes.Writer, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
                    await networkStream.WriteValueAsync((long)written, cancellationToken);
                    await networkStream.WriteAsync(signatureBytes.ReaderMemory[..written], cancellationToken);
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
#else
                Thread.Yield();
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

    private static void FlushStream(TcpClient client, NetworkStream stream)
    {
        Span<byte> buffer = stackalloc byte[256];
        while (client.Available > 0)
        {
            _ = stream.Read(buffer);
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
//         var timer = Stopwatch.StartNew();
//         while (client.Available < sizeof(long))
//         {
// #if DEBUG
//                 await Task.Delay(100, cancellationToken);
// #else
//             Thread.Yield();
// #endif
//             if (timer.ElapsedMilliseconds > ConnectionSettings!.Value.Timeout)
//                 throw new TimeoutException();
//         }
//
//         var outStreamSize = await clientStream.ReadLongAsync(cancellationToken);
//         timer.Restart();
//         while (client.Available < outStreamSize)
//         {
// #if DEBUG
//                 await Task.Delay(100, cancellationToken);
// #else
//             Thread.Yield();
// #endif
//             if (timer.ElapsedMilliseconds > ConnectionSettings!.Value.Timeout)
//                 throw new TimeoutException();
//         }
//         _ = await clientStream.ReadAsync(_shortOutStream.AsMemory()[..(int)outStreamSize], cancellationToken);
//         _shortOutStream.Position = 0;
        var faulted = (byte)clientStream.ReadByte();
        if (faulted != 0) throw new FaultedRequestException();
        var inserted = clientStream.ReadInt();
        return inserted;
    }

    private async Task<int> BulkInsertSerializableInternalAsync<T>(IEnumerable<T> values,
        CancellationToken cancellationToken = default) where T : IAstraSerializable
    {
        
        var (client, clientStream, reversed) = _client ?? throw new NotConnectedException();
        FlushStream(client, clientStream);
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

        if (count == 0) return 0;

        await clientStream.WriteValueAsync(InsertHeaderSize + _inStream.Position, cancellationToken);
        await clientStream.WriteValueAsync(Command.CreateWriteHeader(1U), cancellationToken); // 1 command
        await clientStream.WriteValueAsync(Command.UnsortedInsert, cancellationToken); // Command type (insert)
        await clientStream.WriteValueAsync(count, cancellationToken); // `count` rows
        await clientStream.WriteAsync(new ReadOnlyMemory<byte>(_inStream.GetBuffer(),
            0, (int)_inStream.Position), cancellationToken);
        // await client.WaitForData<TimeoutException>(sizeof(long), ConnectionSettings!.Value.Timeout);
        //
        // var outStreamSize = await clientStream.ReadLongAsync(cancellationToken);
        // await client.WaitForData<TimeoutException>(outStreamSize, ConnectionSettings!.Value.Timeout);
        //
        // _ = await clientStream.ReadAsync(_shortOutStream.AsMemory()[..(int)outStreamSize], cancellationToken);
        // _shortOutStream.Position = 0;
        var faulted = (byte)clientStream.ReadByte();
        if (faulted != 0) throw new FaultedRequestException();
        var inserted = clientStream.ReadInt();
        return inserted;
    }
    
    Task<int> IAstraClient.BulkInsertSerializableAsync<T>(IEnumerable<T> values, CancellationToken cancellationToken)
    {
        return BulkInsertSerializableInternalAsync(values, cancellationToken);
    }

    public Task<int> BulkInsertSerializableCompatAsync<T>(IEnumerable<T> values,
        CancellationToken cancellationToken = default) where T : IAstraSerializable
    {
        return BulkInsertSerializableInternalAsync(values, cancellationToken);
    }
    
    public Task<int> BulkInsertAsync<T>(IEnumerable<T> values,
        CancellationToken cancellationToken = default)
    {
        return BulkInsertSerializableInternalAsync(values.Select(o => new FlexSerializable<T> { Target = o }),
            cancellationToken);
    }

    private async ValueTask SendPredicateInternal(ReadOnlyMemory<byte> predicateStream,
        CancellationToken cancellationToken = default)
    {
        var (_, clientStream, _) = _client ?? throw new NotConnectedException();
        // FlushStream(client, clientStream);
        await clientStream.WriteValueAsync(HeaderSize + predicateStream.Length, cancellationToken);
        await clientStream.WriteValueAsync(Command.CreateReadHeader(1U), cancellationToken); // 1 command
        await clientStream.WriteValueAsync(Command.UnsortedAggregate, cancellationToken); // Command type (aggregate)
        await clientStream.WriteAsync(predicateStream, cancellationToken);
        // await client.WaitForData<TimeoutException>(sizeof(long), ConnectionSettings!.Value.Timeout, cancellationToken: cancellationToken);
        // var outStreamSize = await clientStream.ReadLongAsync(cancellationToken);
        // var outCluster = BytesCluster.Rent((int)outStreamSize);
        // await client.WaitAndRead<TimeoutException>(outCluster.WriterMemory, ConnectionSettings!.Value.Timeout, cancellationToken);
        // var stream = outCluster.Promote();
        var faulted = (byte)clientStream.ReadByte();
        if (faulted != 0) throw new FaultedRequestException();
    }
    
    private async ValueTask<ResultsSet<T>> AggregateCompatInternalAsync<T>(ReadOnlyMemory<byte> predicateStream, CancellationToken cancellationToken = default) where T : IAstraSerializable
    {
        await SendPredicateInternal(predicateStream, cancellationToken);
        return new(this, ConnectionSettings!.Value.Timeout);
    }
    
    private async ValueTask<DynamicResultsSet<T>> AggregateInternalAsync<T>(ReadOnlyMemory<byte> predicateStream, CancellationToken cancellationToken = default)
    {
        await SendPredicateInternal(predicateStream, cancellationToken);
        return new(this, ConnectionSettings!.Value.Timeout);
    }

    public ValueTask<ResultsSet<T>> AggregateCompatAsync<T>(IAstraQueryBranch predicate,
        CancellationToken cancellationToken = default) where T : IAstraSerializable
    {
        return AggregateCompatInternalAsync<T>(predicate.DumpMemory(), cancellationToken);
    }
    
    public ValueTask<DynamicResultsSet<T>> AggregateAsync<T>(IAstraQueryBranch predicate,
        CancellationToken cancellationToken = default)
    {
        return AggregateInternalAsync<T>(predicate.DumpMemory(), cancellationToken);
    }
    
    public ValueTask<ResultsSet<T>> AggregateCompatAsync<T>(GenericAstraQueryBranch predicate,
        CancellationToken cancellationToken = default) where T : IAstraSerializable
    {
        return AggregateCompatInternalAsync<T>(predicate.DumpMemory(), cancellationToken);
    }
    
    public ValueTask<DynamicResultsSet<T>> AggregateAsync<T>(GenericAstraQueryBranch predicate,
        CancellationToken cancellationToken = default)
    {
        return AggregateInternalAsync<T>(predicate.DumpMemory(), cancellationToken);
    }
    
    public ValueTask<ResultsSet<T>> AggregateCompatAsync<T, TPredicate>(TPredicate predicate,
        CancellationToken cancellationToken = default) 
        where T : IAstraSerializable
        where TPredicate : IAstraQueryBranch
    {
        return AggregateCompatInternalAsync<T>(predicate.DumpMemory(), cancellationToken);
    }
    
    public ValueTask<DynamicResultsSet<T>> AggregateAsync<T, TPredicate>(TPredicate predicate,
        CancellationToken cancellationToken = default) 
        where TPredicate : IAstraQueryBranch
    {
        return AggregateInternalAsync<T>(predicate.DumpMemory(), cancellationToken);
    }
    
    Task<IEnumerable<T>> IAstraClient.AggregateAsync<T>(IAstraQueryBranch predicate, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public async Task<int> CountAllAsync(CancellationToken cancellationToken = default)
    {
        var (client, clientStream, _) = _client ?? throw new NotConnectedException();
        await clientStream.WriteValueAsync(HeaderSize, cancellationToken);
        await clientStream.WriteValueAsync(Command.CreateReadHeader(1U), cancellationToken); // 1 command
        await clientStream.WriteValueAsync(Command.CountAll, cancellationToken); // Command type (count all)
//         var timer = Stopwatch.StartNew();
//         while (client.Available < sizeof(long))
//         {
// #if DEBUG
//                 await Task.Delay(100, cancellationToken);
// #else
//             Thread.Yield();
// #endif
//             if (timer.ElapsedMilliseconds > ConnectionSettings!.Value.Timeout)
//                 throw new TimeoutException();
//         }
//
//         var outStreamSize = await clientStream.ReadLongAsync(cancellationToken);
//         timer.Restart();
//         while (client.Available < outStreamSize)
//         {
// #if DEBUG
//                 await Task.Delay(100, cancellationToken);
// #else
//             Thread.Yield();
// #endif
//             if (timer.ElapsedMilliseconds > ConnectionSettings!.Value.Timeout)
//                 throw new TimeoutException();
//         }
//         var outCluster = BytesCluster.Rent((int)outStreamSize);
//         await clientStream.ReadExactlyAsync(outCluster.WriterMemory, cancellationToken);
//         using var stream = outCluster.Promote();
        var faulted = (byte)clientStream.ReadByte();
        if (faulted != 0) throw new FaultedRequestException();
        return clientStream.ReadInt();
    }

    private async Task<int> ConditionalCountInternalAsync(ReadOnlyMemory<byte> predicateStream, CancellationToken cancellationToken = default)
    {
        var (client, clientStream, _) = _client ?? throw new NotConnectedException();
        await clientStream.WriteValueAsync(HeaderSize + predicateStream.Length, cancellationToken);
        await clientStream.WriteValueAsync(Command.CreateReadHeader(1U), cancellationToken); // 1 command
        await clientStream.WriteValueAsync(Command.ConditionalCount, cancellationToken); // Command type (conditional count)
        await clientStream.WriteAsync(predicateStream, cancellationToken);
//         var timer = Stopwatch.StartNew();
//         while (client.Available < sizeof(long))
//         {
// #if DEBUG
//                 await Task.Delay(100, cancellationToken);
// #else
//             Thread.Yield();
// #endif
//             if (timer.ElapsedMilliseconds > ConnectionSettings!.Value.Timeout)
//                 throw new TimeoutException();
//         }
//
//         var outStreamSize = await clientStream.ReadLongAsync(cancellationToken);
//         timer.Restart();
//         while (client.Available < outStreamSize)
//         {
// #if DEBUG
//                 await Task.Delay(100, cancellationToken);
// #else
//             Thread.Yield();
// #endif
//             if (timer.ElapsedMilliseconds > ConnectionSettings!.Value.Timeout)
//                 throw new TimeoutException();
//         }
//
//         var outCluster = BytesCluster.Rent((int)outStreamSize);
//         await clientStream.ReadExactlyAsync(outCluster.WriterMemory, cancellationToken);
//         using var stream = outCluster.Promote();
        var faulted = (byte)clientStream.ReadByte();
        if (faulted != 0) throw new FaultedRequestException();
        return clientStream.ReadInt();
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
//         var timer = Stopwatch.StartNew();
//         while (client.Available < sizeof(long))
//         {
// #if DEBUG
//                 await Task.Delay(100, cancellationToken);
// #else
//             Thread.Yield();
// #endif
//             if (timer.ElapsedMilliseconds > ConnectionSettings!.Value.Timeout)
//                 throw new TimeoutException();
//         }
//
//         var outStreamSize = await clientStream.ReadLongAsync(cancellationToken);
//         timer.Restart();
//         while (client.Available < outStreamSize)
//         {
// #if DEBUG
//                 await Task.Delay(100, cancellationToken);
// #else
//             Thread.Yield();
// #endif
//             if (timer.ElapsedMilliseconds > ConnectionSettings!.Value.Timeout)
//                 throw new TimeoutException();
//         }
//
//         var outCluster = BytesCluster.Rent((int)outStreamSize);
//         await clientStream.ReadExactlyAsync(outCluster.WriterMemory, cancellationToken);
//         using var stream = outCluster.Promote();
        var faulted = (byte)clientStream.ReadByte();
        if (faulted != 0) throw new FaultedRequestException();
        return clientStream.ReadInt();
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
//         var timer = Stopwatch.StartNew();
//         while (client.Available < sizeof(long))
//         {
// #if DEBUG
//                 await Task.Delay(100, cancellationToken);
// #else
//             Thread.Yield();
// #endif
//             if (timer.ElapsedMilliseconds > ConnectionSettings!.Value.Timeout)
//                 throw new TimeoutException();
//         }
//
//         var outStreamSize = await clientStream.ReadLongAsync(cancellationToken);
//         timer.Restart();
//         while (client.Available < outStreamSize)
//         {
// #if DEBUG
//                 await Task.Delay(100, cancellationToken);
// #else
//             Thread.Yield();
// #endif
//             if (timer.ElapsedMilliseconds > ConnectionSettings!.Value.Timeout)
//                 throw new TimeoutException();
//         }
//         var outCluster = BytesCluster.Rent((int)outStreamSize);
//         await clientStream.ReadExactlyAsync(outCluster.WriterMemory, cancellationToken);
//         using var stream = outCluster.Promote();
        var faulted = (byte)clientStream.ReadByte();
        if (faulted != 0) throw new FaultedRequestException();
        return clientStream.ReadInt();
    }
}