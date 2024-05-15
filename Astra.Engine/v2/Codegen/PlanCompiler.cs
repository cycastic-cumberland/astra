using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Astra.Common.Data;
using Astra.Engine.v2.Data;
using Astra.Engine.v2.Indexers;
using Astra.TypeErasure.Data;
using Astra.TypeErasure.Planners.Physical;

namespace Astra.Engine.v2.Codegen;

file static class PlanCompilerHelper
{
    private static readonly AssemblyName PlanCompilerAssemblyName = new("PlanCompilerAssembly");
    private static readonly AssemblyBuilder PlanCompilerAssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
        PlanCompilerAssemblyName,
        AssemblyBuilderAccess.Run);
    private static readonly ModuleBuilder PlanCompilerModuleBuilder =
        PlanCompilerAssemblyBuilder.DefineDynamicModule("PlanCompilerModule");

    private static readonly MethodInfo IntersectSelector =
        new Func<IEnumerable<DataRow>?, IEnumerable<DataRow>?, IEnumerable<DataRow>?>(Data.Aggregator.IntersectSelect)
            .Method;
    private static readonly MethodInfo UnionSelector =
        new Func<IEnumerable<DataRow>?, IEnumerable<DataRow>?, IEnumerable<DataRow>?>(Data.Aggregator.UnionSelect)
            .Method;
    private static readonly MethodInfo GetHost =
        typeof(BaseIndexer.IReadable).GetProperty(nameof(BaseIndexer.IReadable.Host))?.GetMethod ??
        throw new UnreachableException();
    
    private static TypeBuilder CreateType(ref readonly PhysicalPlan plan)
    {
        var timestamp = unchecked((ulong)Stopwatch.GetTimestamp());
        var name = $"Codegen{plan.GetHashCode()}_{timestamp}";
        
        var builder = PlanCompilerModuleBuilder.DefineType(name,
            TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed);

        var interfaceType = typeof(IQueryExecutor);
        builder.AddInterfaceImplementation(interfaceType);
        
        return builder; 
    }

    private static void LoadCell(ILGenerator ilGenerator, LocalBuilder localCell, LocalBuilder localBytes, ref readonly DataCell cell)
    {
        switch (cell.CellType)
        {
            case DataCell.CellTypes.DWord:
            {
                ilGenerator.Emit(OpCodes.Ldloca_S, localCell);
                ilGenerator.Emit(OpCodes.Ldc_I4, cell.DWord);
                ilGenerator.Emit(OpCodes.Call, typeof(DataCell).GetConstructor([typeof(int)]) ?? throw new UnreachableException());
                break;
            }
            case DataCell.CellTypes.QWord:
            {
                ilGenerator.Emit(OpCodes.Ldloca_S, localCell);
                ilGenerator.Emit(OpCodes.Ldc_I8, cell.QWord);
                ilGenerator.Emit(OpCodes.Call, typeof(DataCell).GetConstructor([typeof(long)]) ?? throw new UnreachableException());
                break;
            }
            case DataCell.CellTypes.Single:
            {
                ilGenerator.Emit(OpCodes.Ldloca_S, localCell);
                ilGenerator.Emit(OpCodes.Ldc_R4, cell.Single);
                ilGenerator.Emit(OpCodes.Call, typeof(DataCell).GetConstructor([typeof(float)]) ?? throw new UnreachableException());
                break;
            }
            case DataCell.CellTypes.Double:
            {
                ilGenerator.Emit(OpCodes.Ldloca_S, localCell);
                ilGenerator.Emit(OpCodes.Ldc_R8, cell.Double);
                ilGenerator.Emit(OpCodes.Call, typeof(DataCell).GetConstructor([typeof(double)]) ?? throw new UnreachableException());
                break;
            }
            case DataCell.CellTypes.Text:
            {
                ilGenerator.Emit(OpCodes.Ldloca_S, localCell);
                ilGenerator.Emit(OpCodes.Ldstr, cell.GetString());
                ilGenerator.Emit(OpCodes.Call, typeof(DataCell).GetConstructor([typeof(string)]) ?? throw new UnreachableException());
                break;
            }
            case DataCell.CellTypes.Bytes:
            {
                var array = cell.ExtractBytes();
                ilGenerator.Emit(OpCodes.Ldc_I4, array.Length);
                ilGenerator.Emit(OpCodes.Newarr, typeof(byte));
                ilGenerator.Emit(OpCodes.Stloc_S, localBytes);
                for (var i = 0; i < array.Length; i++)
                {
                    ilGenerator.Emit(OpCodes.Ldloc_S, localBytes);
                    ilGenerator.Emit(OpCodes.Ldc_I4, i);
                    ilGenerator.Emit(OpCodes.Ldc_I4, array[i]);
                    ilGenerator.Emit(OpCodes.Stelem_I1);
                }
                
                ilGenerator.Emit(OpCodes.Ldloca_S, localCell);
                ilGenerator.Emit(OpCodes.Ldloc_S, localBytes);
                ilGenerator.Emit(OpCodes.Call, typeof(DataCell).GetConstructor([typeof(byte[])]) ?? throw new UnreachableException());
                
                break;
            }
            default:
                throw new UnreachableException();
        }
    }

    private static void TestStuff(ref readonly ReadOnlySpan<int?> span)
    {
        var a = PlanCompiler.GetSpanItem(in span, 0);
        _ = a;
    }

    private static MethodInfo GetSpanIndexer(Type generic)
    {
        return typeof(PlanCompiler).GetMethod(nameof(PlanCompiler.GetSpanItem))?.MakeGenericMethod(generic) ??
               throw new UnreachableException();
    }

    private static void CompileInternal(ref readonly PhysicalPlan plan, ShinDataRegistry registry, ILGenerator ilGenerator, Type generic)
    {
        var localLhs = ilGenerator.DeclareLocal(typeof(IEnumerable<DataRow>));
        var localRhs = ilGenerator.DeclareLocal(typeof(IEnumerable<DataRow>));
        var localCell1 = ilGenerator.DeclareLocal(typeof(DataCell));
        var localCell2 = ilGenerator.DeclareLocal(typeof(DataCell));
        var localBytes = ilGenerator.DeclareLocal(typeof(byte[]));
        var localIndexer = ilGenerator.DeclareLocal(generic);
        
        ilGenerator.Emit(OpCodes.Ldnull);
        ilGenerator.Emit(OpCodes.Stloc_S, localLhs);
        ilGenerator.Emit(OpCodes.Ldnull);
        ilGenerator.Emit(OpCodes.Stloc_S, localRhs);

        var blueprints = plan.Blueprints;
        var rStore = false;
        for (var i = blueprints.Length - 1; i >= 0; i--)
        {
            ref readonly var blueprint = ref blueprints[i];
            switch (blueprint.QueryOperationType)
            {
                case QueryType.IntersectMask:
                {
                    ilGenerator.Emit(OpCodes.Ldloc_S, localLhs);
                    ilGenerator.Emit(OpCodes.Ldloc_S, localRhs);
                    ilGenerator.Emit(OpCodes.Call, IntersectSelector);
                    ilGenerator.Emit(OpCodes.Stloc_S, localRhs);
                    ilGenerator.Emit(OpCodes.Ldnull);
                    ilGenerator.Emit(OpCodes.Stloc_S, localLhs);
                    rStore = true;
                    break;
                }
                case QueryType.UnionMask:
                {
                    ilGenerator.Emit(OpCodes.Ldloc_S, localLhs);
                    ilGenerator.Emit(OpCodes.Ldloc_S, localRhs);
                    ilGenerator.Emit(OpCodes.Call, UnionSelector);
                    ilGenerator.Emit(OpCodes.Stloc_S, localRhs);
                    ilGenerator.Emit(OpCodes.Ldnull);
                    ilGenerator.Emit(OpCodes.Stloc_S, localLhs);
                    rStore = true;
                    break;
                }
                case QueryType.FilterMask:
                {
                    var indexer = registry.Indexers[blueprint.Offset];
                    if (indexer == null) break;
                    var filterMethod = indexer.GetFetchImplementation(blueprint.PredicateOperationType);
                    var filterMethodParams = filterMethod.GetParameters();
                    var cell = blueprint.Cell1;
                    LoadCell(ilGenerator, localCell1, localBytes, ref cell);
                    if (filterMethodParams.Length > 1)
                    {
                        if (filterMethodParams.Length != 2) throw new NotSupportedException();
                        cell = blueprint.Cell2;
                        LoadCell(ilGenerator, localCell2, localBytes, ref cell);
                    }
                    
                    ilGenerator.Emit(OpCodes.Ldarga_S, 1);
                    ilGenerator.Emit(OpCodes.Ldc_I4, blueprint.Offset);
                    ilGenerator.Emit(OpCodes.Call, GetSpanIndexer(generic));
                    ilGenerator.Emit(OpCodes.Stloc_S, localIndexer);
                    
                    ilGenerator.Emit(OpCodes.Ldloca_S, localIndexer);
                    ilGenerator.Emit(OpCodes.Constrained, generic);
                    ilGenerator.Emit(OpCodes.Callvirt, GetHost);
                    ilGenerator.Emit(OpCodes.Castclass, indexer.GetType());
                    ilGenerator.Emit(OpCodes.Ldloca_S, localCell1);
                    if (filterMethod.GetParameters().Length > 1)
                        ilGenerator.Emit(OpCodes.Ldloca_S, localCell2);
                    ilGenerator.Emit(OpCodes.Callvirt, filterMethod);
                    if (!rStore)
                    {
                        ilGenerator.Emit(OpCodes.Stloc_S, localRhs);
                        rStore = true;
                    }
                    else
                    {
                        ilGenerator.Emit(OpCodes.Stloc_S, localLhs);
                    }
                    
                    break;
                }
                default:
                    throw new NotSupportedException();
            }
        }
        
        ilGenerator.Emit(OpCodes.Ldloc_S, localRhs);
        ilGenerator.Emit(OpCodes.Ret);
    }

    public static IQueryExecutor Compile(ref readonly PhysicalPlan plan, ShinDataRegistry registry)
    {
        var typeBuilder = CreateType(in plan);
        var executorMethod = typeBuilder.DefineMethod(nameof(IQueryExecutor.Execute),
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig);
        var genericBuilder = executorMethod.DefineGenericParameters(["T"])[0];
        genericBuilder.SetGenericParameterAttributes(GenericParameterAttributes.NotNullableValueTypeConstraint);
        genericBuilder.SetInterfaceConstraints([typeof(BaseIndexer.IReadable)]);
        
        executorMethod.SetReturnType(typeof(IEnumerable<DataRow>));
        executorMethod.SetParameters([typeof(ReadOnlySpan<>).MakeGenericType(typeof(Nullable<>).MakeGenericType(genericBuilder))]);
        CompileInternal(in plan, registry, executorMethod.GetILGenerator(), genericBuilder);
        var dynamicType = typeBuilder.CreateType();
        var obj = Activator.CreateInstance(dynamicType) ?? throw new NullReferenceException();
        return (IQueryExecutor)obj;
    }
}

public static class PlanCompiler
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T GetSpanItem<T>(ref readonly ReadOnlySpan<T?> span, int index) where T : struct
    {
        return span[index]!.Value;
    }
    public static IQueryExecutor Compile(ref readonly PhysicalPlan plan, ShinDataRegistry registry) => PlanCompilerHelper.Compile(in plan, registry);
}