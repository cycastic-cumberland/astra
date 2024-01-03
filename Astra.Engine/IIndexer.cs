namespace Astra.Engine;

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
        public HashSet<ImmutableDataRow>? Fetch(Stream predicateStream);
    }
    public interface IIndexerWriteHandler : IIndexerReadHandler, ITransaction
    {
        public void Add(ImmutableDataRow row);
        public bool RemoveExact(ImmutableDataRow row);
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