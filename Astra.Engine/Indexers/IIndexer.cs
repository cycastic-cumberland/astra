using System.Collections;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Astra.Common;
using Astra.Common.Data;
using Astra.Common.Protocols;
using Astra.Engine.Aggregator;
using Astra.Engine.Data;

namespace Astra.Engine.Indexers;

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
        public IEnumerable<ImmutableDataRow>? Fetch(Expression expression);
        public IEnumerable<ImmutableDataRow>? Fetch(uint operation, Stream predicateStream);
    }
    public interface IIndexerWriteHandler : IIndexerReadHandler, ITransaction
    {
        public void Add(ImmutableDataRow row);
        public bool RemoveExact(ImmutableDataRow row);
        public void Clear();
    }
    public IIndexerReadHandler Read();
    public IIndexerWriteHandler Write();
    
    public FeaturesList SupportedReadOperations { get; }
    public FeaturesList SupportedWriteOperations { get; }
    public uint Priority { get; }
    public DataType Type { get; }
}

public interface IIndexer<out TR, out TW> : IIndexer
    where TR : IIndexer.IIndexerReadHandler
    where TW : IIndexer.IIndexerWriteHandler
{
    public new TR Read();
    public new TW Write();
}