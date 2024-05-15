using System.Collections;
using System.Diagnostics;
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
            QueryType.IntersectMask => new IntersectAggregator().ParseStream(predicateStream, indexersLock),
            QueryType.UnionMask => new UnionAggregator().ParseStream(predicateStream, indexersLock),
            QueryType.FilterMask => new UnaryAggregator().ParseStream(predicateStream, indexersLock),
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

public struct PreparedLocalAggregatorEnumerator<T, TIndexerLock> : IEnumerator<T>
    where TIndexerLock : struct, DataRegistry.IIndexersLock
    where T : IStreamSerializable
{
    private readonly IEnumerable<ImmutableDataRow> _enumerable;
    private IEnumerator<ImmutableDataRow> _enumerator = null!;
    private TIndexerLock _indexerLock;
    private T _current = default!;
    private RecyclableMemoryStream _buffer = null!;
    private int _stage;

    public PreparedLocalAggregatorEnumerator(TIndexerLock indexerLock, IEnumerable<ImmutableDataRow> enumerable)
    {
        _indexerLock = indexerLock;
        _enumerable = enumerable;
        _stage = 1;
    }

    public void Dispose()
    {
        _enumerator?.Dispose();
        _buffer?.Dispose();
        _buffer = null!;
    }
    
    public bool MoveNext()
    {
        switch (_stage)
        {
            case 1:
            {
                _buffer = MemoryStreamPool.Allocate();
                _enumerator = _enumerable.GetEnumerator();
                _stage = 2;
                goto case 2;
            }
            case 2:
            {
                if (!_enumerator.MoveNext()) goto case 3;
                _buffer.Position = 0;
                for (var i = 0; i < _indexerLock.Count; i++)
                {
                    var t = (_buffer, _enumerator.Current);
                    _indexerLock.Read(i, t, LocalAggregatorEnumerator.ProcessDelegate);
                }
                _buffer.Position = 0;
                var value = Activator.CreateInstance<T>();
                value.DeserializeStream(new ForwardStreamWrapper(_buffer));
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

public struct LocalAggregatorEnumerator<T, TIndexerLock> : IEnumerator<T>
    where TIndexerLock : struct, DataRegistry.IIndexersLock
    where T : IStreamSerializable
{
    private readonly Stream _predicateStream;
    private readonly TIndexerLock _indexerLock;
    private PreparedLocalAggregatorEnumerator<T, TIndexerLock> _enumerator;
    private IEnumerable<ImmutableDataRow>? _result;
    private bool _enumerating;
    private int _stage;

    public LocalAggregatorEnumerator(Stream stream, TIndexerLock indexerLock)
    {
        _predicateStream = stream;
        _indexerLock = indexerLock;
        _stage = 1;
    }

    public void Dispose()
    {
        if (_enumerating)
            _enumerator.Dispose();
        _enumerating = false;
    }
    
    public bool MoveNext()
    {
        switch (_stage)
        {
            case 1:
            {
                var result = _predicateStream.Aggregate(_indexerLock);
                if (result == null)
                {
                    _stage = 0;
                    return false;
                }

                _result = result;
                _enumerator = new(_indexerLock, _result);
                _enumerating = true;
                _stage = 2;
                goto case 2;
            }
            case 2:
            {
                return _enumerator.MoveNext();
            }
            default:
                return false;
        }
    }

    public void Reset()
    {
        Dispose();
        if (_result == null)
        {
            _stage = 1;
            return;
        }
        _enumerator = new(_indexerLock, _result);
        _enumerating = true;
        _stage = 2;
    }

    public T Current => _enumerator.Current;

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
            outStream.WriteValue(CommonProtocol.EndOfSetFlag);
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
        outStream.WriteValue(CommonProtocol.EndOfSetFlag);
    }
}
