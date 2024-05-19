namespace Astra.Common.Data;

public enum CompressionOptions : byte
{
    None = ConnectionFlags.CompressionOptions.None,
    GZipOptimal = ConnectionFlags.CompressionOptions.GZip | ConnectionFlags.CompressionOptions.Optimal,
    DeflateOptimal = ConnectionFlags.CompressionOptions.Deflate | ConnectionFlags.CompressionOptions.Optimal,
    BrotliOptimal = ConnectionFlags.CompressionOptions.Brotli | ConnectionFlags.CompressionOptions.Optimal,
    ZLibOptimal = ConnectionFlags.CompressionOptions.ZLib | ConnectionFlags.CompressionOptions.Optimal,
    LZ4Optimal = ConnectionFlags.CompressionOptions.LZ4 | ConnectionFlags.CompressionOptions.Optimal,
    GZipFastest = ConnectionFlags.CompressionOptions.GZip | ConnectionFlags.CompressionOptions.Fastest,
    DeflateFastest = ConnectionFlags.CompressionOptions.Deflate | ConnectionFlags.CompressionOptions.Fastest,
    BrotliFastest = ConnectionFlags.CompressionOptions.Brotli | ConnectionFlags.CompressionOptions.Fastest,
    ZLibFastest = ConnectionFlags.CompressionOptions.ZLib | ConnectionFlags.CompressionOptions.Fastest,
    LZ4Fastest = ConnectionFlags.CompressionOptions.LZ4 | ConnectionFlags.CompressionOptions.Fastest,
    GZipSmallestSize = ConnectionFlags.CompressionOptions.GZip | ConnectionFlags.CompressionOptions.SmallestSize,
    DeflateSmallestSize = ConnectionFlags.CompressionOptions.Deflate | ConnectionFlags.CompressionOptions.SmallestSize,
    BrotliSmallestSize = ConnectionFlags.CompressionOptions.Brotli | ConnectionFlags.CompressionOptions.SmallestSize,
    ZLibSmallestSize = ConnectionFlags.CompressionOptions.ZLib | ConnectionFlags.CompressionOptions.SmallestSize,
    LZ4SmallestSize = ConnectionFlags.CompressionOptions.LZ4 | ConnectionFlags.CompressionOptions.SmallestSize,
}