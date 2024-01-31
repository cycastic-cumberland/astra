using System.Runtime.CompilerServices;
using Astra.Common;
using Astra.Engine.Resolvers;

namespace Astra.Engine.Data;

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
            foreach (var resolver in resolvers)
            {
                resolver.Destroy(this);
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
    private BytesClusterStream? _hashStream;
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

    public ImmutableDataRow Consume<T>(T synthesizers) where T : IEnumerable<ColumnSynthesizer>
    {
        if (_disposed) throw new ObjectDisposedException($"{nameof(DataRow)} consumed");
        _disposed = true;
        if (_hashStream != null)
        {
            try
            {
                var sBuffer = _hashStream.AsSpan();
                var ret = new ImmutableDataRow(_raw, Hash128.HashXx128(sBuffer));
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
        foreach (var synthesizer in synthesizers)
        {
            synthesizer.Resolver.BeginHash(stream, this);
        }
        var buffer = stream.GetBuffer();
        return new(_raw, Hash128.HashMd5(new ReadOnlySpan<byte>(buffer, 0, (int)stream.Length)));
    }

    private DataRow(BytesCluster raw, BytesClusterStream? hashStream = null)
    {
        _raw = raw;
        _hashStream = hashStream;
    }

    public static DataRow Create<T>(Stream reader, T synthesizers, int rawSize, int hashSize) where T : IEnumerable<ColumnSynthesizer>
    {
        var hashStream = BytesCluster.Rent(hashSize).Promote();
        try
        {
            var row = new DataRow(BytesCluster.Rent(rawSize), hashStream);
            foreach (var synthesizer in synthesizers)
            {
                // DataRow only hold a single reference so no need to worry
                synthesizer.Resolver.Initialize(reader, hashStream, row);
            }

            return row;
        }
        catch (Exception)
        {
            hashStream.Dispose();
            throw;
        }
    }

    private static readonly ThreadLocal<BytesClusterStream?> LocalHashStream = new();
    
    public static ImmutableDataRow CreateImmutable<T>(Stream reader, T synthesizers, int rawSize, int hashSize) where T : IEnumerable<ColumnSynthesizer>
    {
        var hashStream = LocalHashStream.Value;
        if (hashStream == null)
        {
            hashStream = BytesCluster.Rent(hashSize).Promote();
        }
        else
        {
            if (hashStream.Length < hashSize)
            {
                hashStream.Dispose();
                hashStream = BytesCluster.Rent(hashSize).Promote();
            }
        }

        var rowBuffer = BytesCluster.Rent(rawSize);
        try
        {
            var row = new DataRow(rowBuffer, hashStream);
            foreach (var synthesizer in synthesizers)
            {
                // DataRow only hold a single reference so no need to worry
                synthesizer.Resolver.Initialize(reader, hashStream, row);
            }

            var sBuffer = hashStream.AsSpan()[..hashSize];
            var ret = new ImmutableDataRow(rowBuffer, Hash128.HashXx128(sBuffer));

            return ret;
        }
        catch (Exception)
        {
            rowBuffer.Dispose();
            throw;
        }
        finally
        {
            hashStream.Position = 0;
            LocalHashStream.Value = hashStream;
        }
    }

    public void Load<T>(Stream reader, T synthesizers) where T : IEnumerable<ColumnSynthesizer>
    {
        _hashStream?.Dispose();
        _hashStream = null;
        foreach (var synthesizer in synthesizers)
        {
            synthesizer.Resolver.Deserialize(reader, this);
        }
    }

    public ReadOnlySpan<byte> Read
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _raw.Reader;
    }
    public bool IsImmutable => false;

    public void Save<T>(Stream writer, T synthesizers) where T : IEnumerable<ColumnSynthesizer>
    {
        foreach (var synthesizer in synthesizers)
        {
            // ImmutableDataRow is read-only so no need to worry about passing values
            // Also its size is not too big (at least 24-byte, 32 if aligned)
            synthesizer.Resolver.Serialize(writer, this);
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