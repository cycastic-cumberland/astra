using System.Buffers;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Astra.Engine.v2.Data;

public class DataRow : IDisposable, IEquatable<DataRow>
{
    private readonly ColumnSchema[] _tableSchema;
    private readonly DataCell[] _pool;
    private readonly int _length;
    private readonly int _cachedHash;

    public ReadOnlySpan<DataCell> Span => new(_pool, 0, _length);

    public DataRow(DataCell[] pool, ColumnSchema[] tableSchema)
    {
        _tableSchema = tableSchema;
        _pool = pool;
        _length = tableSchema.Length;
        _cachedHash = CalculateHash();
    }
    
    public void Dispose()
    {
        ArrayPool<DataCell>.Shared.Return(_pool);
    }

    private int CalculateHash()
    {
        Span<byte> hashesSpan = stackalloc byte[sizeof(int) * _length];
        var span = Span;
        for (var i = 0; i < span.Length; i++)
        {
            var hash = 0;
            if (_tableSchema[i].ShouldBeHashed)
            {
                ref readonly var value = ref span[i];
                hash = value.GetHashCode();
            }
            unsafe
            {
                new ReadOnlySpan<byte>(&hash, sizeof(int)).CopyTo(hashesSpan[(i * sizeof(int))..]);
            }
        }

        return unchecked((int)XxHash32.HashToUInt32(hashesSpan));
    }

    public bool Equals(DataRow? other)
    {
        if (other == null) return false;
        var mySpan = Span;
        var theirSpan = other.Span;
        if (mySpan.Length != theirSpan.Length) return false;
        for (var i = 0; i < _length; i++)
        {
            if (!_tableSchema[i].ShouldBeHashed) continue;
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

    public static DataRow Deserialize(ColumnSchema[] tableSchema, Stream stream)
    {
        var array = ArrayPool<DataCell>.Shared.Rent(tableSchema.Length);
        for (var i = 0; i < tableSchema.Length; i++)
        {
            var schema = tableSchema[i];
            var cell = DataCell.FromStream(schema.Type.Value, stream);
            array[i] = cell;
        }

        return new(array, tableSchema);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as DataRow);
    }
}