using Astra.Engine.Data;

namespace Astra.Engine.Indexers;

public interface IPointIndexer
{
    public interface IPointIndexerReadHandler : IDisposable
    {
        public HashSet<ImmutableDataRow>? CollectExact(Stream predicateStream);
    }
    public interface IPointIndexerWriteHandler : IPointIndexerReadHandler
    {
        public void Add(ImmutableDataRow row);
        public HashSet<ImmutableDataRow>? Remove(Stream predicateStream);
        public bool RemoveExact(ImmutableDataRow row);
    }

    public IPointIndexerReadHandler Read();
    public IPointIndexerWriteHandler Write();
}

public interface IPointIndexer<out TR, out TW> : IPointIndexer
    where TR : struct, IPointIndexer.IPointIndexerReadHandler
    where TW : struct, IPointIndexer.IPointIndexerWriteHandler
{
    public new TR Read();
    public new TW Write();
}

public interface IPointIndexer<in T>
{
    public interface IPointIndexerReadHandler : IDisposable
    {
        public HashSet<ImmutableDataRow>? CollectExact(T match);
    }
    public interface IPointIndexerWriteHandler : IPointIndexerReadHandler
    {
        public HashSet<ImmutableDataRow>? Remove(T match);
    }
    public IPointIndexerReadHandler Read();
    public IPointIndexerWriteHandler Write();
}

public interface IPointIndexer<in T, out TR, out TW> : IPointIndexer<T>
    where TR : struct, IPointIndexer.IPointIndexerReadHandler
    where TW : struct, IPointIndexer.IPointIndexerWriteHandler
{
    public new TR Read();
    public new TW Write();
}
