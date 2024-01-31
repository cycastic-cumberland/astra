using Astra.Engine.Data;

namespace Astra.Engine.Indexers;

public class OperationNotSupported(string? msg = null) : Exception(msg);

public interface ITransaction : IDisposable
{
    public void Commit();
    public void Rollback();
}

public interface IIndexer
{
    public interface IIndexerReadHandler : IDisposable, IEnumerable<ImmutableDataRow>
    {
        public bool Contains(ImmutableDataRow row);
        public IEnumerable<ImmutableDataRow>? Fetch(Stream predicateStream);
    }
    public interface IIndexerWriteHandler : IIndexerReadHandler, ITransaction
    {
        public void Add(ImmutableDataRow row);
        public bool RemoveExact(ImmutableDataRow row);
        public void Clear();
    }
    public IIndexerReadHandler Read();
    public IIndexerWriteHandler Write();
}

public interface IIndexer<out TR, out TW> : IIndexer
    where TR : struct, IIndexer.IIndexerReadHandler
    where TW : struct, IIndexer.IIndexerWriteHandler
{
    public new TR Read();
    public new TW Write();
}