using Astra.Engine.v2.Data;
using Astra.Engine.v2.Indexers;
using Astra.TypeErasure.Data;
using Astra.TypeErasure.Planners.Physical;

namespace Astra.Engine.v2.Codegen;

public struct FallbackExecutor : IQueryExecutor
{
    public PhysicalPlan Plan;
    
    public IEnumerable<DataRow>? Execute<T>(ReadOnlySpan<T?> readers) where T : struct, BaseIndexer.IReadable
    {
        // return Data.Aggregator.ApplyPhysicalPlan(ref Plan, ref readers);
        return null;
    }

    public IEnumerable<DataRow>? ExampleExecutor<T>(ReadOnlySpan<T?> readers) where T : struct, BaseIndexer.IReadable
    {
        IEnumerable<DataRow>? lhs = null;
        IEnumerable<DataRow>? rhs = null;
        DataCell cell1;
        DataCell cell2;
        T indexer;

        cell1 = new(2);
        indexer = PlanCompiler.GetSpanItem(ref readers, 0);
        rhs = ((NumericIndexer)indexer.Host).LessThan(ref cell1);

        cell1 = new(5);
        indexer = PlanCompiler.GetSpanItem(ref readers, 0);
        lhs = ((NumericIndexer)indexer.Host).GreaterThan(ref cell1);

        rhs = Data.Aggregator.UnionSelect(lhs, rhs);
        lhs = null;

        cell1 = new(9);
        cell2 = new(19);
        indexer = PlanCompiler.GetSpanItem(ref readers, 1);
        lhs = ((NumericIndexer)indexer.Host).ClosedBetween(ref cell1, ref cell2);

        rhs = Data.Aggregator.IntersectSelect(lhs, rhs);
        lhs = null;
        return rhs;
    }
}