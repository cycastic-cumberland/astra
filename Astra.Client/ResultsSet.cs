using System.Buffers;
using System.Collections;
using System.Net.Sockets;
using Astra.Common.Data;
using Astra.Common.Protocols;
using Astra.Common.StreamUtils;

namespace Astra.Client;

public class ConstraintCheckFailedException(string? msg = null) : Exception(msg);

public class ResultsSet<T> : IEnumerable<T>, IDisposable
    where T : IStreamSerializable
{
    public readonly struct Enumerator : IEnumerator<T>
    {
        private readonly ResultsSet<T> _host;

        public Enumerator(ResultsSet<T> host)
        {
            if (host._exclusivity)
                throw new Exceptions.ConcurrencyException("Multiple readers cannot exist at the same time");
            host._exclusivity = true;
            _host = host;
        }

        public void Dispose()
        {
            _host._exclusivity = false;
        }

        public bool MoveNext() => _host.MoveNext();

        public void Reset()
        {
            throw new NotSupportedException();
        }

        public T Current => _host.Current;

        object IEnumerator.Current => Current;
    }
    
    private readonly Stream _stream;
    private readonly AstraClient.ExclusivityCheck _exclusivityCheck;
    private readonly ReadOnlyMemory<uint>? _constraint;
    private bool _exclusivity;
    private int _stage;
    private T _current = default!;
    private (uint type, string name)[] _array;
    private int _columnCount;

    public ReadOnlySpan<(uint type, string name)> Columns => new(_array, 0, _columnCount);

    public ResultsSet(AstraClient astraClient, int timeout, ReadOnlyMemory<uint>? constraintCheck = null)
    {
        if (astraClient.Client == null) throw new Exceptions.NotConnectedException();
        var client = astraClient.Client.GetValueOrDefault();
        var reader = client.Reader;
        var reversed = client.ShouldReverse;
        _exclusivityCheck = new(astraClient);
        _constraint = constraintCheck;
        _stream = reader;
        _array = [];
        _stage = reversed ? 3 : 1;
    }

    public void Dispose()
    {
        try
        {
            while (MoveNext())
            {
                
            }
        }
        finally
        {
            if (_array.Length > 0)
                CleanUpPool();
            _exclusivityCheck.Dispose();
        }

    }

    private bool PerformConstraintCheck()
    {
        if (_constraint == null) return true;
        var constraint = _constraint.Value.Span;
        if (_columnCount != constraint.Length) return false;
        for (var i = 0; i < constraint.Length; i++)
        {
            if (_array[i].type != constraint[i]) return false;
        }

        return true;
    }
    
    private void CleanUpPool()
    {
        ArrayPool<(uint, string)>.Shared.Return(_array);
        _array = Array.Empty<(uint, string)>();
    }
    
    private T Current => _current;

    private bool MoveNext()
    {
        try
        {
            switch (_stage)
            {
                case 1:
                {
                    var stream = new ForwardStreamWrapper(_stream);
                    _columnCount = stream.LoadInt();
                    _array = ArrayPool<(uint, string)>.Shared.Rent(_columnCount);
                    for (var i = 0; i < _columnCount; i++)
                    {
                        var type = stream.LoadUInt();
                        var name = stream.LoadString();
                        _array[i] = (type, name);
                    }

                    if (!PerformConstraintCheck()) throw new ConstraintCheckFailedException();
                    var flag = stream.LoadByte();
                    if (flag != CommonProtocol.HasRow) goto case -1;
                    _stage = 2;
                    goto case 2;
                }
                case 2:
                {
                    var stream = new ForwardStreamWrapper(_stream);
                    var value = Activator.CreateInstance<T>();
                    value.DeserializeStream(stream);
                    _current = value;
                    var flag = stream.LoadByte();
                    if (flag != CommonProtocol.ChainedFlag) _stage = -1;
                    return true;
                }
                case 3:
                {
                    var stream = new ReverseStreamWrapper(_stream);
                    _columnCount = stream.LoadInt();
                    _array = ArrayPool<(uint, string)>.Shared.Rent(_columnCount);
                    for (var i = 0; i < _columnCount; i++)
                    {
                        var type = stream.LoadUInt();
                        var name = stream.LoadString();
                        _array[i] = (type, name);
                    }

                    if (!PerformConstraintCheck()) throw new ConstraintCheckFailedException();
                    var flag = stream.LoadByte();
                    if (flag != CommonProtocol.EndOfSetFlag) goto case -1;
                    _stage = 4;
                    goto case 4;
                }
                case 4:
                {
                    var stream = new ReverseStreamWrapper(_stream);
                    var value = Activator.CreateInstance<T>();
                    value.DeserializeStream(stream);
                    _current = value;
                    var flag = stream.LoadByte();
                    if (flag != CommonProtocol.ChainedFlag) _stage = -1;
                    return true;
                }
                case -1:
                {
                    CleanUpPool();
                    _stage = 0;
                    goto default;
                }
                default: return false;
            }
        }
        catch
        {
            CleanUpPool();
            _stage = 0;
            throw;
        }
    }

    public Enumerator GetEnumerator()
    {
        return new(this);
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