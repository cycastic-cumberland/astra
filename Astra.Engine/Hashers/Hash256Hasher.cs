using Astra.Collections.WideDictionary;
using Astra.Common;
using Astra.Common.Hashes;

namespace Astra.Engine.Hashers;

public sealed class Hash256Hasher : UnmanagedTypeHasher<Hash256>
{
    public static readonly Hash256Hasher Default = new();
}