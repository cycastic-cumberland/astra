using System.Buffers;
using System.Collections;
using Astra.Common.Protocols;
using Astra.Common.StreamUtils;

namespace Astra.Common.Data;

public struct AstraSerializableCursor<T, TStream> : IEnumerator<T> 
    where T : IAstraSerializable
    where TStream : Stream
{
    private readonly TStream _stream;
    private readonly int _disposeStream;
    private int _stage;
    private T _current = default!;
    private string[] _array;
    private int _columnCount;
    
    public AstraSerializableCursor(TStream stream, bool reversed, bool doesDispose)
    {
        _stream = stream;
        _array = Array.Empty<string>();
        _stage = 1;
        _disposeStream = doesDispose ? 1 : 0;
        _stage = reversed ? 3 : 1;
    }
    
    public void Dispose()
    {
        while (MoveNext())
        {
            
        }
        _stage = -3 + _disposeStream;
        MoveNext();
    }

    private void CleanUpPool()
    {
        ArrayPool<string>.Shared.Return(_array);
        _array = Array.Empty<string>();
    }

    public bool MoveNext()
    {
        try
        {
            switch (_stage)
            {
                case 1:
                {
                    var stream = new ForwardStreamWrapper(_stream);
                    _columnCount = stream.LoadInt();
                    _array = ArrayPool<string>.Shared.Rent(_columnCount);
                    for (var i = 0; i < _columnCount; i++)
                    {
                        var name = stream.LoadString();
                        _array[i] = name;
                    }
                    
                    var flag = stream.LoadInt();
                    if (flag != CommonProtocol.HasRow) goto case -1;
                    _stage = 2;
                    goto case 2;
                }
                case 2:
                {
                    var stream = new ForwardStreamWrapper(_stream);
                    var value = Activator.CreateInstance<T>();
                    value.DeserializeStream(stream, _array.AsSpan()[.._columnCount]);
                    _current = value;
                    var flag = stream.LoadInt();
                    if (flag != CommonProtocol.ChainedFlag) _stage = -1;
                    return true;
                }
                case 3:
                {
                    var stream = new ReverseStreamWrapper(_stream);
                    _columnCount = stream.LoadInt();
                    _array = ArrayPool<string>.Shared.Rent(_columnCount);
                    for (var i = 0; i < _columnCount; i++)
                    {
                        var name = stream.LoadString();
                        _array[i] = name;
                    }
                    
                    var flag = stream.LoadInt();
                    if (flag != CommonProtocol.HasRow) goto case -1;
                    _stage = 4;
                    goto case 4;
                }
                case 4:
                {
                    var stream = new ReverseStreamWrapper(_stream);
                    var value = Activator.CreateInstance<T>();
                    value.DeserializeStream(stream, _array.AsSpan()[.._columnCount]);
                    _current = value;
                    var flag = stream.LoadInt();
                    if (flag != CommonProtocol.ChainedFlag) _stage = -1;
                    return true;
                }
                case -1:
                {
                    CleanUpPool();
                    _stage = 0;
                    goto default;
                }
                case -2:
                {
                    _stream.Dispose();
                    return false;
                }
                default:
                    return false;
            }
        }
        catch
        {
            CleanUpPool();
            _stage = 0;
            throw;
        }
    }

    public void Reset()
    {
        throw new NotSupportedException();
    }

    public T Current => _current;

    object IEnumerator.Current => _current;
}

public readonly struct AstraSerializableEnumerable<T, TStream>(TStream stream, bool isReversed, bool doesDispose) : IEnumerable<T>
    where T : IAstraSerializable
    where TStream : Stream
{
    public AstraSerializableCursor<T, TStream> GetEnumerator()
    {
        return new(stream, isReversed, doesDispose);
    }
    
    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

// public readonly struct WriteForwardBulkInsertHandler<T, TStream>(TStream streamWrapper, IEnumerable<T> enumerable)
//     where T : IAstraSerializable
//     where TStream : IStreamWrapper
// {
//     public async ValueTask InsertAsync(CancellationToken cancellationToken = default)
//     {
//         var hasValue = false;
//         foreach (var value in enumerable)
//         {
//             if (!hasValue)
//             {
//                 hasValue = true;
//                 await streamWrapper.SaveValueAsync(CommonProtocol.HasRow, cancellationToken);
//             }
//             else
//             {
//                 await streamWrapper.SaveValueAsync(CommonProtocol.ChainedFlag, cancellationToken);
//             }
//             value.SerializeStream(streamWrapper);
//         }
//         await streamWrapper.SaveValueAsync(CommonProtocol.EndOfResultsSetFlag, cancellationToken);
//     }
//     
//     public void Insert()
//     {
//         var hasValue = false;
//         foreach (var value in enumerable)
//         {
//             if (!hasValue)
//             {
//                 hasValue = true;
//                 streamWrapper.SaveValue(CommonProtocol.HasRow);
//             }
//             else
//             {
//                 streamWrapper.SaveValue(CommonProtocol.ChainedFlag);
//             }
//             value.SerializeStream(streamWrapper);
//         }
//         streamWrapper.SaveValue(CommonProtocol.EndOfResultsSetFlag);
//     }
// }


public interface IAstraSerializable
{
    public void SerializeStream<TStream>(TStream writer) where TStream : IStreamWrapper;
    public void DeserializeStream<TStream>(TStream reader, ReadOnlySpan<string> columnSequence) where TStream : IStreamWrapper;

    public static AstraSerializableEnumerable<T, TStream> DeserializeStream<T, TStream>(TStream stream, bool isReversed, bool doesDispose = true)
        where T : IAstraSerializable where TStream : Stream
    {
        return new AstraSerializableEnumerable<T, TStream>(stream, isReversed, doesDispose);
    }
}