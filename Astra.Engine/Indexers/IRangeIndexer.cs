using System.Numerics;
using Astra.Engine.Data;

namespace Astra.Engine.Indexers;

public interface IRangeIndexer<TR> where TR : INumber<TR>
{
    public interface IRangeIndexerReadHandler : IDisposable
    {
        public IEnumerable<ImmutableDataRow> ClosedBetween(TR left, TR right);
        public IEnumerable<ImmutableDataRow> GreaterThan(TR left);
        public IEnumerable<ImmutableDataRow> GreaterOrEqualsTo(TR left);
        public IEnumerable<ImmutableDataRow> LesserThan(TR right);
        public IEnumerable<ImmutableDataRow> LesserOrEqualsTo(TR right);
    }

    public IRangeIndexerReadHandler Read();
}
