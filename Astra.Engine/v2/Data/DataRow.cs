using System.Buffers;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Astra.TypeErasure;
using Astra.TypeErasure.Data;

namespace Astra.Engine.v2.Data;

public class DataRow : IDisposable, IEquatable<DataRow>
{
    private readonly DataCell[] _pool;
    private readonly int _length;
    private readonly ulong _rowId;
    private readonly int _cachedHash;

    public ReadOnlySpan<DataCell> Span => new(_pool, 0, _length);

    public DataRow(DataCell[] pool, DatastoreContext context)
    {
        _pool = pool;
        _length = context.TableSchema.Length;
        _rowId = context.NewRowId();
        _cachedHash = _rowId.GetHashCode();
    }
    
    public void Dispose()
    {
        var span = Span;
        for (var i = 0; i < _length; i++)
        {
            ref readonly var cell = ref span[i];
            cell.Dispose();
        }
        ArrayPool<DataCell>.Shared.Return(_pool);
    }

    public bool Equals(DataRow? other)
    {
        return other != null && _rowId == other._rowId;
    }
    
    public bool EqualityCheck(DataRow? other)
    {
        if (other == null) return false;
        var mySpan = Span;
        var theirSpan = other.Span;
        if (mySpan.Length != theirSpan.Length) return false;
        for (var i = 0; i < _length; i++)
        {
            ref readonly var lhs = ref mySpan[i];
            if (!lhs.Equals(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(other._pool), (nint)i))) 
                return false;
        }

        return true;
    }

    public override int GetHashCode() => _cachedHash;

    public void Serialize(Stream stream)
    {
        var span = Span;
        for (var i = 0; i < span.Length; i++)
        {
            ref readonly var cell = ref span[i];
            cell.Write(stream);
        }
    }

    public static DataRow Deserialize(DatastoreContext context, Stream stream)
    {
        var array = ArrayPool<DataCell>.Shared.Rent(context.TableSchema.Length);
        for (var i = 0; i < context.TableSchema.Length; i++)
        {
            var schema = context.TableSchema[i];
            var cell = DataCell.FromStream(schema.Type.Value, stream);
            array[i] = cell;
        }

        return new(array, context);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as DataRow);
    }
}