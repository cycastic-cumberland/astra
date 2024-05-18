using System.Collections;
using Astra.Common.Data;
using Astra.Common.Serializable;

namespace Astra.Client.Simple;

public readonly struct DynamicResultsSet<T> : IEnumerable<T>, IDisposable
{
    private static readonly uint[] CompiledTypeCodes = TypeHelpers.ToAccessibleProperties<T>()
        .Select(o => DataType.DotnetTypeToAstraType(o.PropertyType)).ToArray();
    public readonly struct Enumerator : IEnumerator<T>
    {
        private readonly ResultsSet<FlexSerializable<T>>.Enumerator _host;

        public Enumerator(ResultsSet<FlexSerializable<T>>.Enumerator host)
        {
            _host = host;
        }

        public void Dispose()
        {
            _host.Dispose();
        }

        public bool MoveNext() => _host.MoveNext();

        public void Reset() => _host.Reset();

        public T Current => _host.Current.Target;

        object? IEnumerator.Current => Current;
    }

    private readonly ResultsSet<FlexSerializable<T>> _set;

    public DynamicResultsSet(AstraClient client, int timeout)
    {
        _set = new(client, timeout, CompiledTypeCodes.AsMemory());
    }
    
    public Enumerator GetEnumerator()
    {
        return new(_set.GetEnumerator());
    }
    
    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Dispose()
    {
        _set.Dispose();
    }
}