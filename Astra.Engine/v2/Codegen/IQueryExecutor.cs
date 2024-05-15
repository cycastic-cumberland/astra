using Astra.Engine.v2.Data;
using Astra.Engine.v2.Indexers;

namespace Astra.Engine.v2.Codegen;

public interface IQueryExecutor
{
    public IEnumerable<DataRow>? Execute<T>(ReadOnlySpan<T?> readers) where T : struct, BaseIndexer.IReadable;
}