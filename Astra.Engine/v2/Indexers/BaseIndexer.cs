using System.Collections;
using Astra.Common.Data;
using Astra.Engine.v2.Data;
using Astra.TypeErasure.Data;
using Astra.TypeErasure.Planners;

namespace Astra.Engine.v2.Indexers;

public abstract class BaseIndexer(ColumnSchema schema)
{
    protected readonly ColumnSchema Schema = schema;
    // private readonly RWLock _rwLock = new();

    protected abstract IEnumerator<DataRow> GetEnumerator();
    protected abstract bool Contains(DataRow row);
    protected abstract IEnumerable<DataRow>? Fetch(ref readonly OperationBlueprint blueprint);
    protected abstract IEnumerable<DataRow>? Fetch(Stream predicateStream);
    protected abstract IEnumerable<DataRow>? Fetch(uint operation, Stream predicateStream);
    protected abstract bool Add(DataRow row);
    protected abstract IEnumerable<DataRow>? Remove(Stream predicateStream);
    protected abstract bool Remove(DataRow row);
    protected abstract void Clear();
    protected abstract int Count { get; }
    public abstract FeaturesList SupportedReadOperations { get; }
    public abstract FeaturesList SupportedWriteOperations { get; }
    public abstract uint Priority { get; }
    public abstract DataType Type { get; }

    public interface IReadable : IDisposable, IEnumerable<DataRow>
    {
        public bool Contains(DataRow row);
        public int Count { get; }
        public IEnumerable<DataRow>? Fetch(Stream predicateStream);
        public IEnumerable<DataRow>? Fetch(ref readonly OperationBlueprint blueprint);
        public IEnumerable<DataRow>? Fetch(uint operation, Stream predicateStream);
    }
    
    public readonly struct Reader : IReadable
    {
        private readonly BaseIndexer _host;
        // private readonly RWLock.ReadLockInstance _lock;

        public Reader(BaseIndexer host)
        {
            _host = host;
            // _lock = host._rwLock.Read();
        }

        public void Dispose()
        {
            // _lock.Dispose();
        }

        public IEnumerator<DataRow> GetEnumerator() => _host.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _host.GetEnumerator();

        public bool Contains(DataRow row) => _host.Contains(row);

        public int Count => _host.Count;
        
        public IEnumerable<DataRow>? Fetch(Stream predicateStream) => _host.Fetch(predicateStream);
        
        public IEnumerable<DataRow>? Fetch(ref readonly OperationBlueprint blueprint) => _host.Fetch(in blueprint);
        
        public IEnumerable<DataRow>? Fetch(uint operation, Stream predicateStream) => _host.Fetch(operation, predicateStream);
    }

    public readonly struct Writer : IReadable
    {
        private readonly BaseIndexer _host;
        // private readonly RWLock.WriteLockInstance _lock;

        public Writer(BaseIndexer host)
        {
            _host = host;
            // _lock = host._rwLock.Write();
        }

        public void Dispose()
        {
            // _lock.Dispose();
        }

        public IEnumerator<DataRow> GetEnumerator() => _host.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _host.GetEnumerator();

        public bool Contains(DataRow row) => _host.Contains(row);
        
        public int Count => _host.Count;
        
        public IEnumerable<DataRow>? Fetch(Stream predicateStream) => _host.Fetch(predicateStream);
        
        public IEnumerable<DataRow>? Fetch(ref readonly OperationBlueprint blueprint) => _host.Fetch(in blueprint);
        
        public IEnumerable<DataRow>? Fetch(uint operation, Stream predicateStream) => _host.Fetch(operation, predicateStream);

        public bool Add(DataRow row) => _host.Add(row);
        
        public IEnumerable<DataRow>? Remove(Stream predicateStream) => _host.Remove(predicateStream);
        public void Remove(DataRow predicateStream) => _host.Remove(predicateStream);
        
        public void Clear() => _host.Clear();
    }

    public Reader Read() => new(this);
    public Writer Write() => new(this);
}