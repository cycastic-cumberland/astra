using Microsoft.IO;

namespace Astra.Common;

public static class MemoryStreamPool
{
    private static readonly RecyclableMemoryStreamManager StreamManager = new(new()
    {
        BlockSize = 32
    });

    public static RecyclableMemoryStream Allocate() => StreamManager.GetStream();
    public static RecyclableMemoryStream Allocate(string? tag) => StreamManager.GetStream(tag);
}