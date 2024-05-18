using System.Reflection;
using Astra.Common.Data;
using Astra.Engine.v2.Data;
using Astra.TypeErasure.Data;
using Astra.TypeErasure.Planners.Physical;

namespace Astra.Engine.v2.Indexers;

public abstract class BaseIndexer(ColumnSchema schema)
{
    protected readonly ColumnSchema Schema = schema;
    protected internal readonly RWLock Latch = RWLock.Create();

    protected abstract IEnumerator<DataRow> GetEnumerator();
    protected abstract bool Contains(DataRow row);
    protected abstract HashSet<DataRow>? Fetch(ref readonly OperationBlueprint blueprint);
    protected abstract HashSet<DataRow>? Fetch(Stream predicateStream);
    protected abstract HashSet<DataRow>? Fetch(uint operation, Stream predicateStream);
    protected abstract bool Add(DataRow row);
    protected abstract bool Remove(DataRow row);
    protected abstract void Clear();
    internal abstract MethodInfo GetFetchImplementation(uint operation);
    protected abstract int Count { get; }
    public abstract FeaturesList SupportedReadOperations { get; }
    public abstract FeaturesList SupportedWriteOperations { get; }
    public abstract uint Priority { get; }
    public abstract DataType Type { get; }

    public interface IReadable : IDisposable
    {
        public bool Contains(DataRow row);
        public int Count { get; }
        public IEnumerable<DataRow>? Fetch(Stream predicateStream);
        public IEnumerable<DataRow>? Fetch(ref readonly OperationBlueprint blueprint);
        public IEnumerable<DataRow>? Fetch(uint operation, Stream predicateStream);
        public BaseIndexer Host { get; }
    }

    public readonly struct Reader : IReadable
    {
        private readonly BaseIndexer _host;

        public Reader(BaseIndexer host)
        {
            _host = host;
        }

        public void Dispose()
        {
            
        }
        
        // ReSharper disable once NotDisposedResourceIsReturned
        public IEnumerator<DataRow> GetEnumerator() => _host.GetEnumerator();

        public bool Contains(DataRow row) => _host.Contains(row);

        public int Count => _host.Count;
        
        public IEnumerable<DataRow>? Fetch(Stream predicateStream) => _host.Fetch(predicateStream);
        
        public IEnumerable<DataRow>? Fetch(ref readonly OperationBlueprint blueprint) => _host.Fetch(in blueprint);
        
        public IEnumerable<DataRow>? Fetch(uint operation, Stream predicateStream) => _host.Fetch(operation, predicateStream);
        public BaseIndexer Host => _host;
    }

    public readonly struct Writer : IReadable
    {
        private readonly BaseIndexer _host;

        public Writer(BaseIndexer host)
        {
            _host = host;
        }

        public void Dispose()
        {
            
        }

        // ReSharper disable once NotDisposedResourceIsReturned
        public IEnumerator<DataRow> GetEnumerator() => _host.GetEnumerator();

        public bool Contains(DataRow row) => _host.Contains(row);
        
        public int Count => _host.Count;
        
        public IEnumerable<DataRow>? Fetch(Stream predicateStream) => _host.Fetch(predicateStream);
        
        public IEnumerable<DataRow>? Fetch(ref readonly OperationBlueprint blueprint) => _host.Fetch(in blueprint);
        
        public IEnumerable<DataRow>? Fetch(uint operation, Stream predicateStream) => _host.Fetch(operation, predicateStream);

        public bool Add(DataRow row) => _host.Add(row);

        public void Remove(DataRow predicateStream) => _host.Remove(predicateStream);
        
        public void Clear() => _host.Clear();
        public BaseIndexer Host => _host;
    }

    public Reader Read() => new(this);
    public Writer Write() => new(this);
}