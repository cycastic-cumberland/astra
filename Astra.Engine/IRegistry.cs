using Astra.Common.Data;
using Microsoft.Extensions.Logging;

namespace Astra.Engine;

public interface IRegistry : IDisposable
{
    public int RowsCount { get; }
    public int ColumnCount { get; }
    public int IndexedColumnCount { get; }
    public int Delete(Stream predicateStream);
    public bool Insert<T>(T value);
    public int BulkInsert<T>(IEnumerable<T> values);
    public int Clear();
    public IEnumerable<T> Aggregate<T>(ReadOnlyMemory<byte> predicate);
    public IEnumerable<T> Aggregate<T>(Stream predicate);
    public IEnumerator<T> GetEnumerator<T>();
    public void ConsumeStream<TIn, TOut>(TIn dataIn, TOut dataOut) where TIn : Stream where TOut : Stream;
}

public interface IRegistry<out TSelf> : IRegistry
    where TSelf : IRegistry<TSelf>
{
    public static abstract TSelf Fabricate(RegistrySchemaSpecifications tableSpecification,
        ILoggerFactory? loggerFactory = null,
        AbstractRegistryDump? dump = null);
}