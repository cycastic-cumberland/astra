using Astra.Collections.WideDictionary;
using Astra.Common;
using Astra.Common.Hashes;

namespace Astra.Engine.Hashers;

public sealed class Hash128Hasher : UnmanagedTypeHasher<Hash128>
{
    public static readonly Hash128Hasher Default = new();
}