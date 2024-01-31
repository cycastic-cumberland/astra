using Astra.Collections.WideDictionary;
using Astra.Common;

namespace Astra.Engine.Hashers;

public sealed class Hash256Hasher : UnmanagedTypeHasher<Hash256>
{
    public static readonly Hash256Hasher Default = new();
}