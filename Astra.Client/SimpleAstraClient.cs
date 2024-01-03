using System.Net.Sockets;
using Astra.Engine;

namespace Astra.Client;

public class SimpleAstraClient : IAstraClient
{
    private class EndianModeNotSupportedException(string? msg = null) : NotSupportedException(msg);
    private class FaultedRequestException(string? msg = null) : Exception(msg);
    
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    
    private SimpleAstraClient(AstraConnectionSettings settings)
    {
        ConnectionSettings = settings;
        _client = new TcpClient(settings.Address, settings.Port);
        _stream = _client.GetStream();
    }
    
    public static async Task<SimpleAstraClient> Create(AstraConnectionSettings settings)
    {
        var ret = new SimpleAstraClient(settings);
        var client = ret._client;
        var stream = ret._stream;
        while (client.Available < sizeof(int))
        {
#if DEBUG
            await Task.Delay(100);
#endif
        }
        using var checkEndianness = BytesCluster.Rent(sizeof(int));
        _ = await stream.ReadAsync(checkEndianness.WriterMemory);
        var isLittleEndian = checkEndianness.Reader[0] == 1;
        if (isLittleEndian != BitConverter.IsLittleEndian)
            throw new EndianModeNotSupportedException($"Endianness not supported: {(isLittleEndian ? "little endian" : "big endian")}");
        
        return ret;
    }
    
    public void Dispose()
    {
        _client.Dispose();
    }
    
    public AstraConnectionSettings ConnectionSettings { get; }
    public bool IsConnected => true;
    
    public async Task<int> UnorderedInsertAsync(MemoryStream dataStream, bool autoDisposeStream = false, CancellationToken cancellationToken = default)
    {
        // Automatically assume 1 command, UnsortedInsert
        const long headerSize = sizeof(uint) + sizeof(uint); // 1U + Command.UnsortedInsert
        var streamLength = dataStream.Length - dataStream.Position;
        if (streamLength == 0)
            throw new EndOfStreamException("End of stream reached. Did you forgot to unwind the stream?");
        try
        {
            await _stream.WriteValueAsync(headerSize + streamLength, token: cancellationToken);
            await _stream.WriteValueAsync(1U, token: cancellationToken);
            await _stream.WriteValueAsync(Command.UnsortedInsert, token: cancellationToken);
            await _stream.WriteAsync(new ReadOnlyMemory<byte>(dataStream.GetBuffer(),
                (int)dataStream.Position, (int)streamLength), cancellationToken);
            while (_client.Available < sizeof(long))
            {
#if DEBUG
                await Task.Delay(100, cancellationToken);
#endif
            }
            var outStreamSize = await _stream.ReadLongAsync(token: cancellationToken);
            var cluster = BytesCluster.Rent((int)outStreamSize);
            while (_client.Available < outStreamSize) await Task.Delay(100, cancellationToken);
            _ = await _stream.ReadAsync(cluster.WriterMemory, cancellationToken);
            await using var outStream = cluster.Promote();
            var faulted = (byte)outStream.ReadByte();
            if (faulted != 0) throw new FaultedRequestException();
            var inserted = outStream.ReadInt();
            return inserted;
        }
        finally
        {
            if (autoDisposeStream) 
                await dataStream.DisposeAsync();
        }
    }
}