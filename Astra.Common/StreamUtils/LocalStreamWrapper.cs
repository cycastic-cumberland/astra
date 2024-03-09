using Astra.Common.Protocols;
using Microsoft.IO;

namespace Astra.Common.StreamUtils;

public readonly struct LocalStreamWrapper : IDisposable
{
    private static readonly ThreadLocal<RecyclableMemoryStream?> ThreadLocalStream = new();
    public readonly RecyclableMemoryStream LocalStream;

    private LocalStreamWrapper(RecyclableMemoryStream stream)
    {
        LocalStream = stream;
    }

    public static LocalStreamWrapper Create()
    {
        var stream = ThreadLocalStream.Value ?? MemoryStreamPool.Allocate();
        ThreadLocalStream.Value = null;
        return new(stream);
    }

    public void Dispose()
    {
        if (ThreadLocalStream.Value != null || 
            LocalStream.Length > CommonProtocol.ThreadLocalStreamDisposalThreshold)
        {
            LocalStream.Dispose();
        }
        else
        {
            LocalStream.SetLength(0);
            ThreadLocalStream.Value = LocalStream;
        }
    }
}