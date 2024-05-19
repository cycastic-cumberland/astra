using System.Diagnostics;
using System.IO.Compression;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Astra.Client.Aggregator;
using Astra.Common.Data;
using Astra.Common.Hashes;
using Astra.Common.Protocols;
using Astra.Common.Serializable;
using Astra.Common.StreamUtils;
using Astra.TypeErasure.Planners.Physical;
using Microsoft.IO;

namespace Astra.Client;


// Side job: handle endianness 
public class AstraClient : IAstraClient
{
    internal readonly struct InternalClient(
        TcpClient client,
        NetworkStream clientStream,
        bool reversedOrder,
        ConnectionFlags connectionFlags) : IDisposable
    {
        private readonly Stream _reader = GetReader(ref connectionFlags, clientStream);
        private readonly Stream _writer = GetWriter(ref connectionFlags, clientStream);
        public TcpClient Client
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => client;
        }
        
        public Stream Reader
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _reader;
        }

        public Stream Writer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _writer;
        }
        
        public bool ShouldReverse
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => reversedOrder;
        }
        
        public ConnectionFlags ConnectionFlags
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => connectionFlags;
        }

        private static Stream GetReader(ref readonly ConnectionFlags flags, NetworkStream stream)
        {
            return flags.CompressionAlgorithmRaw switch
            {
                ConnectionFlags.CompressionOptions.GZip => new GZipStream(stream, CompressionMode.Decompress),
                ConnectionFlags.CompressionOptions.Deflate => new DeflateStream(stream, CompressionMode.Decompress),
                ConnectionFlags.CompressionOptions.Brotli => new BrotliStream(stream, CompressionMode.Decompress),
                ConnectionFlags.CompressionOptions.ZLib => new ZLibStream(stream, CompressionMode.Decompress),
                _ => stream
            };
        }

        private static Stream GetWriter(ref readonly ConnectionFlags flags, NetworkStream stream)
        {
            var strategy = flags.CompressionStrategyRaw;
            switch (flags.CompressionAlgorithmRaw)
            {
                case ConnectionFlags.CompressionOptions.GZip:
                {
                    GZipStream writer;
                    if ((strategy & ConnectionFlags.CompressionOptions.Fastest) > 0)
                        writer = new GZipStream(stream, CompressionLevel.Fastest);
                    else if ((strategy & ConnectionFlags.CompressionOptions.SmallestSize) > 0)
                        writer = new GZipStream(stream, CompressionLevel.Fastest);
                    else
                        writer = new GZipStream(stream, CompressionLevel.Optimal);
                    return writer;
                }
                case ConnectionFlags.CompressionOptions.Deflate:
                {
                    DeflateStream writer;
                    if ((strategy & ConnectionFlags.CompressionOptions.Fastest) > 0)
                        writer = new DeflateStream(stream, CompressionLevel.Fastest);
                    else if ((strategy & ConnectionFlags.CompressionOptions.SmallestSize) > 0)
                        writer = new DeflateStream(stream, CompressionLevel.Fastest);
                    else
                        writer = new DeflateStream(stream, CompressionLevel.Optimal);
                    return writer;
                }
                case ConnectionFlags.CompressionOptions.Brotli:
                {
                    BrotliStream writer;
                    if ((strategy & ConnectionFlags.CompressionOptions.Fastest) > 0)
                        writer = new BrotliStream(stream, CompressionLevel.Fastest);
                    else if ((strategy & ConnectionFlags.CompressionOptions.SmallestSize) > 0)
                        writer = new BrotliStream(stream, CompressionLevel.Fastest);
                    else
                        writer = new BrotliStream(stream, CompressionLevel.Optimal);
                    return writer;
                }
                case ConnectionFlags.CompressionOptions.ZLib:
                {
                    ZLibStream writer;
                    if ((strategy & ConnectionFlags.CompressionOptions.Fastest) > 0)
                        writer = new ZLibStream(stream, CompressionLevel.Fastest);
                    else if ((strategy & ConnectionFlags.CompressionOptions.SmallestSize) > 0)
                        writer = new ZLibStream(stream, CompressionLevel.Fastest);
                    else
                        writer = new ZLibStream(stream, CompressionLevel.Optimal);
                    return writer;
                }
                default:
                    return new WriteForwardBufferedStream(stream);
            }
        }

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public void Deconstruct(out TcpClient outClient, out NetworkStream outClientStream, out bool reversed)
        // {
        //     outClient = client;
        //     outClientStream = clientStream;
        //     reversed = reversedOrder;
        // }
        //
        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public void Deconstruct(out TcpClient outClient, out NetworkStream outClientStream, out bool reversed, out ConnectionFlags flags)
        // {
        //     outClient = client;
        //     outClientStream = clientStream;
        //     reversed = reversedOrder;
        //     flags = connectionFlags;
        // }

        public readonly bool IsConnected = true;
        public void Dispose()
        {
            client.Close();
        }
    }
    
    internal readonly struct ExclusivityCheck : IDisposable
    {
        private readonly AstraClient _client;
        public ExclusivityCheck(AstraClient client)
        {
            if (client._exclusivity)
                throw new Exceptions.ConcurrencyException("Multiple readers cannot exist at the same time");
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

    public AstraClientConnectionSettings? ConnectionSettings { get; private set; }
    public bool IsConnected => _client?.IsConnected ?? false;

    public async Task ConnectAsync(AstraClientConnectionSettings settings, CancellationToken cancellationToken = default)
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
            await networkStream.ReadExactlyAsync(outBuffer.WriterMemory[..sizeof(int)], cancellationToken);
            var isLittleEndian = outBuffer.Reader[0] == 1;
            if (isLittleEndian != BitConverter.IsLittleEndian)
                throw new Exceptions.EndianModeNotSupportedException($"Endianness not supported: {(isLittleEndian ? "little endian" : "big endian")}");
            await networkStream.ReadExactlyAsync(outBuffer.WriterMemory[..sizeof(ulong)], cancellationToken);
            var handshakeSignal = BitConverter.ToUInt64(outBuffer.Reader[..sizeof(ulong)]);
            if (handshakeSignal != CommunicationProtocol.ServerIdentification)
            {
                throw new Exceptions.HandshakeFailedException();
            }
            await networkStream.ReadExactlyAsync(outBuffer.WriterMemory[..sizeof(uint)], cancellationToken);
            var serverVersion = BitConverter.ToUInt32(outBuffer.Reader[..sizeof(uint)]);
            if (serverVersion != CommonProtocol.AstraCommonVersion) 
                throw new Exceptions.VersionNotSupportedException($"Astra.Server version not supported: {serverVersion.ToAstraCommonVersion()}");

            await networkStream.ReadExactlyAsync(outBuffer.WriterMemory[..sizeof(uint)], cancellationToken);
            var connectionFlags = ConnectionFlags.From(BitConverter.ToUInt32(outBuffer.Reader[..sizeof(uint)]));
            
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
                        throw new Exceptions.AuthenticationInfoNotProvidedException(nameof(settings.Password));
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
                        throw new Exceptions.AuthenticationInfoNotProvidedException(nameof(settings.PrivateKey));
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
                    throw new Exceptions.AuthenticationMethodNotSupportedException($"Authentication method not supported: {authMethod}");
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
                throw new Exceptions.AuthenticationAttemptRejectedException();
            networkClient.NoDelay = true;
            _client = new(networkClient, networkStream, false, connectionFlags);
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
    
    public Task<int> InsertSerializableAsync<T>(T value, CancellationToken cancellationToken = default) where T : IStreamSerializable
    {
        return BulkInsertSerializableInternalAsync([value], cancellationToken);
    }

    private static void WriteInBulk<T, TStream>(IEnumerable<T> values, TStream stream) 
        where T : IStreamSerializable
        where TStream : IStreamWrapper
    {
        var flag = CommonProtocol.HasRow;
        foreach (var value in values)
        {
            stream.SaveValue(flag);
            flag = CommonProtocol.ChainedFlag;
            value.SerializeStream(stream);
        }
        stream.SaveValue(CommonProtocol.EndOfSetFlag);
    }

    private async Task<int> BulkInsertSerializableInternalAsync<T>(IEnumerable<T> values,
        CancellationToken cancellationToken = default) where T : IStreamSerializable
    {
        if (_client == null) throw new Exceptions.NotConnectedException();
        var client = _client.GetValueOrDefault();
        var writer = client.Writer;
        var reader = client.Reader;
        var reversed = client.ShouldReverse;
        await writer.WriteValueAsync(InsertHeaderSize + _inStream.Position, cancellationToken);
        await writer.WriteValueAsync(Command.CreateWriteHeader(1U), cancellationToken); // 1 command
        await writer.WriteValueAsync(Command.UnsortedInsert, cancellationToken); // Command type (insert)
        if (reversed)
        {
            var wrapped = new ReverseStreamWrapper(writer);
            WriteInBulk(values, wrapped);
        }
        else
        {
            var wrapped = new ForwardStreamWrapper(writer);
            WriteInBulk(values, wrapped);
        }
        writer.Flush();
        var faulted = (byte)reader.ReadByte();
        if (faulted != 0) throw new Exceptions.FaultedRequestException();
        var inserted = reader.ReadInt();
        return inserted;
    }
    
    Task<int> IAstraClient.BulkInsertSerializableAsync<T>(IEnumerable<T> values, CancellationToken cancellationToken)
    {
        return BulkInsertSerializableInternalAsync(values, cancellationToken);
    }

    public Task<int> BulkInsertSerializableCompatAsync<T>(IEnumerable<T> values,
        CancellationToken cancellationToken = default) where T : IStreamSerializable
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
        if (_client == null) throw new Exceptions.NotConnectedException();
        var client = _client.GetValueOrDefault();
        var writer = client.Writer;
        var reader = client.Reader;
        await writer.WriteValueAsync(HeaderSize + predicateStream.Length, cancellationToken);
        await writer.WriteValueAsync(Command.CreateReadHeader(1U), cancellationToken); // 1 command
        await writer.WriteValueAsync(Command.UnsortedAggregate, cancellationToken); // Command type (aggregate)
        await writer.WriteAsync(predicateStream, cancellationToken);
        writer.Flush();
        var faulted = (byte)reader.ReadByte();
        if (faulted != 0) throw new Exceptions.FaultedRequestException();
    }
    
    private async ValueTask<ResultsSet<T>> AggregateCompatInternalAsync<T>(ReadOnlyMemory<byte> predicateStream, CancellationToken cancellationToken = default) where T : IStreamSerializable
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
        CancellationToken cancellationToken = default) where T : IStreamSerializable
    {
        return AggregateCompatInternalAsync<T>(predicate.DumpMemory(), cancellationToken);
    }
    
    public ValueTask<DynamicResultsSet<T>> AggregateAsync<T>(IAstraQueryBranch predicate,
        CancellationToken cancellationToken = default)
    {
        return AggregateInternalAsync<T>(predicate.DumpMemory(), cancellationToken);
    }
    
    public ValueTask<ResultsSet<T>> AggregateCompatAsync<T>(GenericAstraQueryBranch predicate,
        CancellationToken cancellationToken = default) where T : IStreamSerializable
    {
        return AggregateCompatInternalAsync<T>(predicate.DumpMemory(), cancellationToken);
    }
    
    public ValueTask<DynamicResultsSet<T>> AggregateAsync<T>(GenericAstraQueryBranch predicate,
        CancellationToken cancellationToken = default)
    {
        return AggregateInternalAsync<T>(predicate.DumpMemory(), cancellationToken);
    }
    
    public async ValueTask<DynamicResultsSet<T>> AggregateAsync<T>(PhysicalPlan builder,
        CancellationToken cancellationToken = default)
    {
        if (_client == null) throw new Exceptions.NotConnectedException();
        var client = _client.GetValueOrDefault();
        var writer = client.Writer;
        var reader = client.Reader;
        var reversed = client.ShouldReverse;
        var planReversed = client.ConnectionFlags.IsCellBased;
        await writer.WriteValueAsync(HeaderSize, cancellationToken);
        await writer.WriteValueAsync(Command.CreateReadHeader(1U), cancellationToken); // 1 command
        await writer.WriteValueAsync(planReversed ? Command.ReversedPlanAggregate : Command.UnsortedAggregate, 
            cancellationToken);
        if (reversed)
        {
            builder.ToStream(new ReverseStreamWrapper(writer), planReversed);
        }
        else
        {
            builder.ToStream(new ForwardStreamWrapper(writer), planReversed);
        }
        writer.Flush();

        var faulted = (byte)reader.ReadByte();
        if (faulted != 0) throw new Exceptions.FaultedRequestException();
        return new(this, ConnectionSettings!.Value.Timeout);
    }
    
    public ValueTask<ResultsSet<T>> AggregateCompatAsync<T, TPredicate>(TPredicate predicate,
        CancellationToken cancellationToken = default) 
        where T : IStreamSerializable
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
        if (_client == null) throw new Exceptions.NotConnectedException();
        var client = _client.GetValueOrDefault();
        var writer = client.Writer;
        var reader = client.Reader;
        await writer.WriteValueAsync(HeaderSize, cancellationToken);
        await writer.WriteValueAsync(Command.CreateReadHeader(1U), cancellationToken); // 1 command
        await writer.WriteValueAsync(Command.CountAll, cancellationToken); // Command type (count all)
        writer.Flush();
        var faulted = (byte)reader.ReadByte();
        if (faulted != 0) throw new Exceptions.FaultedRequestException();
        return reader.ReadInt();
    }

    private async Task<int> ConditionalCountInternalAsync(ReadOnlyMemory<byte> predicateStream, CancellationToken cancellationToken = default)
    {
        if (_client == null) throw new Exceptions.NotConnectedException();
        var client = _client.GetValueOrDefault();
        var writer = client.Writer;
        var reader = client.Reader;
        await writer.WriteValueAsync(HeaderSize + predicateStream.Length, cancellationToken);
        await writer.WriteValueAsync(Command.CreateReadHeader(1U), cancellationToken); // 1 command
        await writer.WriteValueAsync(Command.ConditionalCount, cancellationToken); // Command type (conditional count)
        await writer.WriteAsync(predicateStream, cancellationToken);
        writer.Flush();
        var faulted = (byte)reader.ReadByte();
        if (faulted != 0) throw new Exceptions.FaultedRequestException();
        return reader.ReadInt();
    }
    
    public Task<int> ConditionalCountAsync<TA>(TA predicate, CancellationToken cancellationToken = default) where TA : IAstraQueryBranch
    {
        return ConditionalCountInternalAsync(predicate.DumpMemory(), cancellationToken);
    }
    
    private async Task<int> ConditionalDeleteInternalAsync(ReadOnlyMemory<byte> predicateStream, CancellationToken cancellationToken = default) 
    {
        if (_client == null) throw new Exceptions.NotConnectedException();
        var client = _client.GetValueOrDefault();
        var writer = client.Writer;
        var reader = client.Reader;
        await writer.WriteValueAsync(HeaderSize + predicateStream.Length, cancellationToken);
        await writer.WriteValueAsync(Command.CreateWriteHeader(1U), cancellationToken); // 1 command
        await writer.WriteValueAsync(Command.ConditionalDelete, cancellationToken); // Command type (conditional count)
        await writer.WriteAsync(predicateStream, cancellationToken);
        writer.Flush();
        var faulted = (byte)reader.ReadByte();
        if (faulted != 0) throw new Exceptions.FaultedRequestException();
        return reader.ReadInt();
    }

    public Task<int> ConditionalDeleteAsync<TA>(TA predicate, CancellationToken cancellationToken = default) where TA : IAstraQueryBranch
    {
        return ConditionalDeleteInternalAsync(predicate.DumpMemory(), cancellationToken);
    }

    public async Task<int> ClearAsync(CancellationToken cancellationToken = default)
    {
        if (_client == null) throw new Exceptions.NotConnectedException();
        var client = _client.GetValueOrDefault();
        var writer = client.Writer;
        var reader = client.Reader;
        await writer.WriteValueAsync(HeaderSize, cancellationToken);
        await writer.WriteValueAsync(Command.CreateWriteHeader(1U), cancellationToken); // 1 command
        await writer.WriteValueAsync(Command.Clear, cancellationToken); // Command type (count all)
        writer.Flush();
        var faulted = (byte)reader.ReadByte();
        if (faulted != 0) throw new Exceptions.FaultedRequestException();
        return reader.ReadInt();
    }
}