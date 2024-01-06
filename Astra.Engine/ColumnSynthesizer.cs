using System.Runtime.CompilerServices;

namespace Astra.Engine;

using SynthesizersRead = (IIndexer.IIndexerReadHandler? handler, IColumnResolver resolver);
using SynthesizersWrite = (IIndexer.IIndexerWriteHandler? handler, IColumnResolver resolver);


public readonly struct ColumnSynthesizer(IIndexer? indexer, IColumnResolver resolver)
{
    public IIndexer? Indexer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => indexer;
    }

    public IColumnResolver Resolver
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => resolver;
    }

    public static ColumnSynthesizer Create(IIndexer? indexer, IColumnResolver resolver)
        => new(indexer, resolver);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SynthesizersRead Read()
    {
        return (indexer?.Read(), resolver);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SynthesizersWrite Write()
    {
        return (indexer?.Write(), resolver);
    }
}