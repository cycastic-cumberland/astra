using System.Runtime.InteropServices;

namespace Astra.Engine;

internal static class MurmurHashInterop
{
    private const string MurmurHashLibPath = "libmurmurhash.so";

    [DllImport(MurmurHashLibPath, CallingConvention = CallingConvention.Cdecl)]
    private static extern unsafe void MurmurHash3_x64_128(void* keyPtr, int len, uint seed, void* outPtr);

    public static Hash128 MurmurHash3_x64_128(ReadOnlySpan<byte> inArray, uint seed = 42)
    {
        Span<byte> outSpan = stackalloc byte[Hash128.Size];
        unsafe
        {
            fixed (void* iPtr = &inArray[0], oPtr = &outSpan[0])
            {
                MurmurHash3_x64_128(iPtr, inArray.Length, seed, oPtr);
            }
        }

        return Hash128.Create(outSpan);
    }
}