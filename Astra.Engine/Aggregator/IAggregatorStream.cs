using System.Collections;
using System.Runtime.CompilerServices;
using Astra.Common;
using Astra.Common.Data;
using Astra.Common.Protocols;
using Astra.Common.StreamUtils;
using Astra.Engine.Data;
using Astra.Engine.Indexers;
using Astra.Engine.Resolvers;
using Microsoft.IO;

namespace Astra.Engine.Aggregator;

public class AggregatorNotSupported(string? msg = null) : NotSupportedException(msg);

public interface IAggregatorStream
{
    public IEnumerable<ImmutableDataRow>? ParseStream<T>(Stream predicateStream, T indexersLock)
        where T : struct, DataRegistry.IIndexersLock;

    public static IEnumerable<ImmutableDataRow>? Aggregate<T>(Stream predicateStream, T indexersLock)
        where T : struct, DataRegistry.IIndexersLock
    {
        var type = predicateStream.ReadUInt();
        var ret = type switch
        {
            PredicateType.BinaryAndMask => new IntersectAggregator().ParseStream(predicateStream, indexersLock),
            PredicateType.BinaryOrMask => new UnionAggregator().ParseStream(predicateStream, indexersLock),
            PredicateType.UnaryMask => new UnaryAggregator().ParseStream(predicateStream, indexersLock),
            _ => throw new AggregateException($"Aggregator type not supported: {type}")
        };

        return ret;
    }
}


file static class LocalAggregatorEnumerator
{
    private static void Process((IIndexer.IIndexerReadHandler? handler, IColumnResolver resolver) tuple, 
        (RecyclableMemoryStream _buffer, ImmutableDataRow Current) payload)
    {
        var (_, resolver) = tuple;
        var (buffer, row) = payload;
        resolver.Serialize(buffer, row);
    }
    
    public static readonly
        Action<(IIndexer.IIndexerReadHandler? handler, IColumnResolver resolver), (RecyclableMemoryStream _buffer,
            ImmutableDataRow Current)> ProcessDelegate = Process; 
}
public struct LocalAggregatorEnumerator<T, TIndexerLock> : IEnumerator<T>
    where TIndexerLock : struct, DataRegistry.IIndexersLock
    where T : IAstraSerializable
{
    private readonly Stream _stream;
    private TIndexerLock _indexerLock;
    private readonly string[] _columnNames;
    private T _current = default!;
    private IEnumerator<ImmutableDataRow> _enumerator = null!;
    private RecyclableMemoryStream _buffer = null!;
    private int _stage;

    public LocalAggregatorEnumerator(Stream stream, TIndexerLock indexerLock, string[] columnNames)
    {
        _stream = stream;
        _indexerLock = indexerLock;
        _columnNames = columnNames;
        _stage = 1;
    }

    public void Dispose()
    {
        _enumerator?.Dispose();
        _buffer?.Dispose();
        _enumerator = null!;
        _buffer = null!;
    }
    
    public bool MoveNext()
    {
        switch (_stage)
        {
            case 1:
            {
                var result = _stream.Aggregate(_indexerLock);
                if (result == null)
                {
                    goto case -1;
                }
                _buffer = MemoryStreamPool.Allocate();
                _enumerator = result.GetEnumerator();
                _stage = 2;
                goto case 2;
            }
            case 2:
            {
                if (!_enumerator.MoveNext()) goto case 3;
                for (var i = 0; i < _indexerLock.Count; i++)
                {
                    var t = (_buffer, _enumerator.Current);
                    _indexerLock.Read(i, t, LocalAggregatorEnumerator.ProcessDelegate);
                }
                _buffer.Position = 0;
                var value = Activator.CreateInstance<T>();
                value.DeserializeStream(new ForwardStreamWrapper(_buffer), _columnNames);
                _current = value;
                return true;
            }
            case 3:
            {
                _enumerator.Dispose();
                _buffer.Dispose();
                _enumerator = null!;
                _buffer = null!;
                goto case -1;
            }
            case -1:
            {
                _stage = 0;
                goto default;
            }
            default:
                return false;
        }
    }

    public void Reset()
    {
        Dispose();
        _stage = 1;
    }

    public T Current => _current;

    object IEnumerator.Current => Current;
}

public static class AstraAggregatorHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<ImmutableDataRow>? Aggregate<T>(this Stream predicateStream, T indexersLock)
        where T : struct, DataRegistry.IIndexersLock
        => IAggregatorStream.Aggregate(predicateStream, indexersLock);

    public static void AggregateStream<T>(this Stream predicateStream, Stream outStream, T indexersLock)
        where T : struct, DataRegistry.IIndexersLock
    {
        var result = predicateStream.Aggregate(indexersLock);
        if (result == null)
        {
            outStream.WriteValue(CommonProtocol.EndOfResultsSetFlag);
            return;
        }

        var flag = CommonProtocol.HasRow;
        var resolverCount = indexersLock.Count;
        foreach (var row in result)
        {
            outStream.WriteValue(flag);
            flag = CommonProtocol.ChainedFlag;
            for (var i = 0; i < resolverCount; i++)
            {
                indexersLock.Read(i, (outStream, row), (tuple, enclosed) =>
                {
                    tuple.resolver.Serialize(enclosed.outStream, enclosed.row);
                });
            }
        }
        outStream.WriteValue(CommonProtocol.EndOfResultsSetFlag);
    }
}
