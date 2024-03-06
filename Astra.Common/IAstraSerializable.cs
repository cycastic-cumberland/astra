using System.Buffers;
using System.Collections;

namespace Astra.Common;

public struct AstraSerializableCursor<T, TStream> : IEnumerator<T> 
    where T : IAstraSerializable
    where TStream : Stream
{
    private readonly TStream _stream;
    private readonly bool _reversed;
    private readonly int _disposeStream;
    private int _stage;
    private T _current = default!;
    private string[] _array;
    private int _columnCount;
    private int _count;
    private int _iterator;
    
    public AstraSerializableCursor(TStream stream, bool reversed, bool doesDispose)
    {
        _stream = stream;
        _array = Array.Empty<string>();
        _stage = 1;
        _reversed = reversed;
        _disposeStream = doesDispose ? 1 : 0;
        Reset();
    }
    
    public void Dispose()
    {
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
                    
                    _count = stream.LoadInt();
                    _iterator = -1;
                    _stage = 2;
                    goto case 2;
                }
                case 2:
                {
                    if (++_iterator >= _count) goto case -1;
                    var stream = new ForwardStreamWrapper(_stream);
                    var value = Activator.CreateInstance<T>();
                    value.DeserializeStream(stream, _array.AsSpan()[.._columnCount]);
                    _current = value;
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
                    
                    _count = stream.LoadInt();
                    _iterator = -1;
                    _stage = 4;
                    goto case 4;
                }
                case 4:
                {
                    if (++_iterator >= _count) goto case -1;
                    var stream = new ReverseStreamWrapper(_stream);
                    var value = Activator.CreateInstance<T>();
                    value.DeserializeStream(stream, _array.AsSpan()[.._columnCount]);
                    _current = value;
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
        _stage = _reversed ? 3 : 1;
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