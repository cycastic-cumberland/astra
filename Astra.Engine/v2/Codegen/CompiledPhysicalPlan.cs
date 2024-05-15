using Astra.Engine.v2.Data;
using Astra.TypeErasure.Planners.Physical;

namespace Astra.Engine.v2.Codegen;

public readonly struct CompiledPhysicalPlan : IDisposable
{
    public readonly PhysicalPlan Plan;
    internal readonly IQueryExecutor Executor;
    internal readonly ShinDataRegistry Host;

    internal CompiledPhysicalPlan(PhysicalPlan plan, IQueryExecutor executor, ShinDataRegistry host)
    {
        Plan = plan;
        Executor = executor;
        Host = host;
    }

    public void Dispose()
    {
        Plan.Dispose();
    }
}