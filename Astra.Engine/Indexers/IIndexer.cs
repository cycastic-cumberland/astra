using System.Collections;
using System.Runtime.CompilerServices;
using Astra.Common;
using Astra.Common.Data;
using Astra.Common.Protocols;
using Astra.Engine.Data;

namespace Astra.Engine.Indexers;

public class OperationNotSupported(string? msg = null) : Exception(msg);

public readonly struct FeaturesList(uint[] array) : IReadOnlyList<uint>
{
    public ListEnumerator<uint, FeaturesList> GetEnumerator()
    {
        return new(this);
    }
    
    IEnumerator<uint> IEnumerable<uint>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Count => array.Length;

    public uint this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => array[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator FeaturesList(uint[] array) => new(array);
    
    public static FeaturesList None
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(Array.Empty<uint>());
    }
}

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