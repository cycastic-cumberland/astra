using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Astra.Engine;
using Microsoft.IO;

namespace Astra.Client;

public class SimpleAstraClient : IAstraClient
{
    public class EndianModeNotSupportedException(string? msg = null) : NotSupportedException(msg);
    public class NotConnectedException(string? msg = null) : Exception(msg);
    public class FaultedRequestException(string? msg = null) : Exception(msg);
    private readonly struct InternalClient(TcpClient client, NetworkStream clientStream) : IDisposable
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Deconstruct(out TcpClient outClient, out NetworkStream outClientStream)
        {
            outClient = client;
            outClientStream = clientStream;
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
        ConnectionSettings = settings;
        var networkClient = new TcpClient(settings.Address, settings.Port);
        var networkStream = networkClient.GetStream();
        while (networkClient.Available < sizeof(int))
        {
#if DEBUG
            await Task.Delay(100, cancellationToken);
#endif
        }
        using var checkEndianness = BytesCluster.Rent(sizeof(int));
        _ = await networkStream.ReadAsync(checkEndianness.WriterMemory, cancellationToken);
        var isLittleEndian = checkEndianness.Reader[0] == 1;
        if (isLittleEndian != BitConverter.IsLittleEndian)
            throw new EndianModeNotSupportedException($"Endianness not supported: {(isLittleEndian ? "little endian" : "big endian")}");
        _client = new(networkClient, networkStream);
    }
    public async Task<int> InsertSerializableAsync<T>(T value, CancellationToken cancellationToken = default) where T : IAstraSerializable
    {
        var (client, clientStream) = _client ?? throw new NotConnectedException();
        _inStream.Position = 0;
        value.SerializeStream(_inStream);
        await clientStream.WriteValueAsync(InsertHeaderSize + _inStream.Position, cancellationToken);
        await clientStream.WriteValueAsync(Command.CreateWriteHeader(1U), cancellationToken); // 1 command
        await clientStream.WriteValueAsync(Command.UnsortedInsert, cancellationToken); // Command type (insert)
        await clientStream.WriteValueAsync(1, cancellationToken); // 1 row
        await clientStream.WriteAsync(new ReadOnlyMemory<byte>(_inStream.GetBuffer(),
            0, (int)_inStream.Position), cancellationToken);
        while (client.Available < sizeof(long))
        {
#if DEBUG
                await Task.Delay(100, cancellationToken);
#endif
        }

        var outStreamSize = await clientStream.ReadLongAsync(cancellationToken);
        while (client.Available < outStreamSize)
        {
#if DEBUG
                await Task.Delay(100, cancellationToken);
#endif
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
        var (client, clientStream) = _client ?? throw new NotConnectedException();
        _inStream.Position = 0;
        var count = 0;
        foreach (var value in values)
        {
            value.SerializeStream(_inStream);
            count++;
        }
        await clientStream.WriteValueAsync(InsertHeaderSize + _inStream.Position, cancellationToken);
        await clientStream.WriteValueAsync(Command.CreateWriteHeader(1U), cancellationToken); // 1 command
        await clientStream.WriteValueAsync(Command.UnsortedInsert, cancellationToken); // Command type (insert)
        await clientStream.WriteValueAsync(count, cancellationToken); // `count` rows
        await clientStream.WriteAsync(new ReadOnlyMemory<byte>(_inStream.GetBuffer(),
            0, (int)_inStream.Position), cancellationToken);
        while (client.Available < sizeof(long))
        {
#if DEBUG
                await Task.Delay(100, cancellationToken);
#endif
        }

        var outStreamSize = await clientStream.ReadLongAsync(cancellationToken);
        while (client.Available < outStreamSize)
        {
#if DEBUG
                await Task.Delay(100, cancellationToken);
#endif
        }
        _ = await clientStream.ReadAsync(_shortOutStream.AsMemory()[..(int)outStreamSize], cancellationToken);
        _shortOutStream.Position = 0;
        var faulted = (byte)_shortOutStream.ReadByte();
        if (faulted != 0) throw new FaultedRequestException();
        var inserted = _shortOutStream.ReadInt();
        return inserted;
    }
    public async Task<IEnumerable<T>> SimpleAggregateAsync<T>(ReadOnlyMemory<byte> predicateStream, CancellationToken cancellationToken = default) where T : IAstraSerializable
    {
        var (client, clientStream) = _client ?? throw new NotConnectedException();
        await clientStream.WriteValueAsync(HeaderSize + predicateStream.Length, cancellationToken);
        await clientStream.WriteValueAsync(Command.CreateReadHeader(1U), cancellationToken); // 1 command
        await clientStream.WriteValueAsync(Command.UnsortedAggregate, cancellationToken); // Command type (aggregate)
        await clientStream.WriteAsync(predicateStream, cancellationToken);
        while (client.Available < sizeof(long))
        {
#if DEBUG
                await Task.Delay(100, cancellationToken);
#endif
        }

        var outStreamSize = await clientStream.ReadLongAsync(cancellationToken);
        while (client.Available < outStreamSize)
        {
#if DEBUG
                await Task.Delay(100, cancellationToken);
#endif
        }

        var outCluster = BytesCluster.Rent((int)outStreamSize);
        await clientStream.ReadExactlyAsync(outCluster.WriterMemory, cancellationToken);
        var stream = outCluster.Promote();
        var faulted = (byte)stream.ReadByte();
        if (faulted != 0) throw new FaultedRequestException();
        return IAstraSerializable.DeserializeStream<T, BytesClusterStream>(stream);
    }

    public async Task<int> CountAllAsync(CancellationToken cancellationToken = default)
    {
        var (client, clientStream) = _client ?? throw new NotConnectedException();
        await clientStream.WriteValueAsync(HeaderSize, cancellationToken);
        await clientStream.WriteValueAsync(Command.CreateReadHeader(1U), cancellationToken); // 1 command
        await clientStream.WriteValueAsync(Command.CountAll, cancellationToken); // Command type (count all)
        while (client.Available < sizeof(long))
        {
#if DEBUG
                await Task.Delay(100, cancellationToken);
#endif
        }

        var outStreamSize = await clientStream.ReadLongAsync(cancellationToken);
        while (client.Available < outStreamSize)
        {
#if DEBUG
                await Task.Delay(100, cancellationToken);
#endif
        }
        var outCluster = BytesCluster.Rent((int)outStreamSize);
        await clientStream.ReadExactlyAsync(outCluster.WriterMemory, cancellationToken);
        using var stream = outCluster.Promote();
        var faulted = (byte)stream.ReadByte();
        if (faulted != 0) throw new FaultedRequestException();
        return stream.ReadInt();
    }

    public async Task<int> SimpleConditionalCountAsync(ReadOnlyMemory<byte> predicateStream, CancellationToken cancellationToken = default)
    {
        var (client, clientStream) = _client ?? throw new NotConnectedException();
        await clientStream.WriteValueAsync(HeaderSize + predicateStream.Length, cancellationToken);
        await clientStream.WriteValueAsync(Command.CreateReadHeader(1U), cancellationToken); // 1 command
        await clientStream.WriteValueAsync(Command.UnsortedAggregate, cancellationToken); // Command type (aggregate)
        await clientStream.WriteAsync(predicateStream, cancellationToken);
        while (client.Available < sizeof(long))
        {
#if DEBUG
                await Task.Delay(100, cancellationToken);
#endif
        }

        var outStreamSize = await clientStream.ReadLongAsync(cancellationToken);
        while (client.Available < outStreamSize)
        {
#if DEBUG
                await Task.Delay(100, cancellationToken);
#endif
        }

        var outCluster = BytesCluster.Rent((int)outStreamSize);
        await clientStream.ReadExactlyAsync(outCluster.WriterMemory, cancellationToken);
        using var stream = outCluster.Promote();
        var faulted = (byte)stream.ReadByte();
        if (faulted != 0) throw new FaultedRequestException();
        return stream.ReadInt();
    }
}