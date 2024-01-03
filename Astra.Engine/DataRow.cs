// #define USE_MURMUR3_SO

using System.Runtime.CompilerServices;

namespace Astra.Engine;

public interface IImmutableDataRow
{
    public ReadOnlySpan<byte> Read { get; }
    public bool IsImmutable { get; }
    public void SelectiveDispose<T>(T resolvers) where T : IEnumerable<IDestructibleColumnResolver>;
}

public interface IDataRow : IImmutableDataRow
{
    public Span<byte> Write { get; }
}

public readonly struct ImmutableDataRow(BytesCluster raw, Hash128 hash) : IImmutableDataRow, IEquatable<ImmutableDataRow>
{
    public Hash128 Hash
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => hash;
    }

    public ReadOnlySpan<byte> Read
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => raw.Reader;
    }
    public bool IsImmutable => true;
    
    public void SelectiveDispose<T>(T resolvers) where T : IEnumerable<IDestructibleColumnResolver>
    {
        try
        {
            using var enumerator = resolvers.GetEnumerator();
            while (enumerator.MoveNext())
            {
                enumerator.Current.Destroy(this);
            }
        }
        finally
        {
            raw.Dispose();
        }
    }

    public bool Equals(ImmutableDataRow other)
    {
        return Hash.Equals(other.Hash);
    }

    public override bool Equals(object? obj)
    {
        return obj is ImmutableDataRow other && Equals(other);
    }

    public override int GetHashCode()
    {
        return hash.GetHashCode();
    }

    public static bool operator ==(ImmutableDataRow left, ImmutableDataRow right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ImmutableDataRow left, ImmutableDataRow right)
    {
        return !(left == right);
    }
}

public struct DataRow : IDataRow
{
    private readonly BytesCluster _raw;
    private MemoryStream? _hashStream;
    private bool _disposed;

    public bool Disposed => _disposed;
    
    public void SelectiveDispose<T>(T resolvers) where T : IEnumerable<IDestructibleColumnResolver>
    {
        if (_disposed) return;
        try
        {
            foreach (var resolver in resolvers)
            {
                resolver.Destroy(this);
            }
        }
        finally
        {
            _raw.Dispose();
            _hashStream?.Dispose();
            _disposed = true;
        }
    }
    
    public DataRow Clone<T>(T resolvers) where T : IEnumerable<IColumnResolver>
    {
        using var stream = MemoryStreamPool.Allocate();
        Save(stream, resolvers);
        stream.Position = 0;
        var ret = Create(stream, resolvers, _raw.Count);
        return ret;
    }

    public ImmutableDataRow Consume<T>(T resolvers) where T : IEnumerable<IColumnResolver>
    {
        if (_disposed) throw new ObjectDisposedException($"{nameof(DataRow)} consumed");
        _disposed = true;
        if (_hashStream != null)
        {
            try
            {
                var sBuffer = _hashStream.GetBuffer();
                var ret = new ImmutableDataRow(_raw, Hash128
#if USE_MURMUR3_SO
                    .HashMurmur3
#else
                    .HashXx128
#endif
                        (new ReadOnlySpan<byte>(sBuffer, 0, (int)_hashStream.Length)));
                _hashStream.Dispose();
                _hashStream = null;
                return ret;
            }
            catch (Exception)
            {
                _hashStream?.Dispose();
                throw;
            }
        }
        using var stream = MemoryStreamPool.Allocate();
        foreach (var resolver in resolvers)
        {
            resolver.BeginHash(stream, this);
        }
        var buffer = stream.GetBuffer();
        return new(_raw, Hash128.HashMd5(new ReadOnlySpan<byte>(buffer, 0, (int)stream.Length)));
    }

    private DataRow(BytesCluster raw, MemoryStream? hashStream = null)
    {
        _raw = raw;
        _hashStream = hashStream;
    }

    public static DataRow Create<T>(T resolvers, int rawSize) where T : IEnumerable<IColumnResolver>
    {
        var row = new DataRow(BytesCluster.Rent(rawSize));
        foreach (var resolver in resolvers)
        {
            // DataRow only hold a single reference so no need to worry
            resolver.Initialize(row);
        }

        return row;
    }

    public static DataRow Create<T>(Stream reader, T resolvers, int rawSize) where T : IEnumerable<IColumnResolver>
    {
        var hashStream = MemoryStreamPool.Allocate();
        try
        {
            var row = new DataRow(BytesCluster.Rent(rawSize), hashStream);
            foreach (var resolver in resolvers)
            {
                // DataRow only hold a single reference so no need to worry
                resolver.Initialize(reader, hashStream, row);
            }

            return row;
        }
        catch (Exception)
        {
            hashStream.Dispose();
            throw;
        }
    }

    public void Load<T>(Stream reader, T resolvers) where T : IEnumerable<IColumnResolver>
    {
        _hashStream?.Dispose();
        _hashStream = null;
        foreach (var resolver in resolvers)
        {
            resolver.Deserialize(reader, this);
        }
    }

    public ReadOnlySpan<byte> Read
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _raw.Reader;
    }
    public bool IsImmutable => false;

    public void Save<T>(Stream writer, T resolvers) where T : IEnumerable<IColumnResolver>
    {
        foreach (var resolver in resolvers)
        {
            // ImmutableDataRow is read-only so no need to worry about passing values
            // Also its size is not too big (at least 24-byte, 32 if aligned)
            resolver.Serialize(writer, this);
        }
    }

    public Span<byte> Write
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => !_disposed
            ? _raw.Writer
            : throw new ObjectDisposedException($"{nameof(DataRow)} consumed");
    }
        
}