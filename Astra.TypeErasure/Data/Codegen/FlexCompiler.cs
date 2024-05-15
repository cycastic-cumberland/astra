using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using Astra.Common.Data;

namespace Astra.TypeErasure.Data.Codegen;

file static class FlexCompilerHelpers
{
    private static readonly AssemblyName FlexCompilerAssemblyName = new("FlexCompilerAssembly");
    private static readonly AssemblyBuilder FlexCompilerAssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
        FlexCompilerAssemblyName,
        AssemblyBuilderAccess.Run);
    private static readonly ModuleBuilder FlexCompilerModuleBuilder =
        FlexCompilerAssemblyBuilder.DefineDynamicModule("FlexCompilerModule");
    
    public static TypeBuilder CreateType(Type type)
    {
        var timestamp = unchecked((ulong)Stopwatch.GetTimestamp());
        var name = $"Codegen{type.Name}{timestamp}";
        
        var builder = FlexCompilerModuleBuilder.DefineType(name,
            TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed);

        var interfaceType = typeof(IStatelessCellSerializable<>).MakeGenericType(type);
        builder.AddInterfaceImplementation(interfaceType);
        
        return builder; 
    }
    
    private static MethodInfo GetSpanIndexer()
    {
        var indexer = typeof(Span<DataCell>).GetProperties()
            .First(p => p.GetIndexParameters().Length > 0);
        return indexer.GetMethod ?? throw new UnreachableException();
    }
    private static MethodInfo GetReadOnlySpanIndexer()
    {
        var indexer = typeof(ReadOnlySpan<DataCell>).GetProperties()
            .First(p => p.GetIndexParameters().Length > 0);
        return indexer.GetMethod ?? throw new UnreachableException();
    }

    public static readonly MethodInfo SpanIndexer = GetSpanIndexer();
    public static readonly MethodInfo ReadOnlySpanIndexer = GetReadOnlySpanIndexer();
}

public class NoParameterlessConstructorException(string? msg = null) : Exception(msg);

file static class FlexCompiler<T>
{
    private static IStatelessCellSerializable<T> CompileType()
    {
        if (!typeof(T).IsPublic)
        {
            throw new InvalidOperationException("Generic type `T` must be public");
        }
        if (!typeof(T).IsValueType && (!typeof(T).GetConstructor(Type.EmptyTypes)?.IsPublic ?? false)) 
            throw new NoParameterlessConstructorException($"`{typeof(T).Name}` must have a public parameterless constructor");
        var typeBuilder = FlexCompilerHelpers.CreateType(typeof(T));
        var properties = TypeHelpers.ToAccessibleProperties<T>().ToArray();
        {
            var serializationMethod = typeBuilder.DefineMethod(nameof(IStatelessCellSerializable<T>.SerializeToCells),
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig);
            serializationMethod.SetReturnType(typeof(void));
            serializationMethod.SetParameters([typeof(Span<DataCell>), typeof(T)]);
            if (typeof(T).IsValueType)
                MakeSerializationValueTypeMethod(serializationMethod.GetILGenerator(), properties);
            else
                MakeSerializationMethod(serializationMethod.GetILGenerator(), properties);
        }
        {
            var deserializationMethod = typeBuilder.DefineMethod(nameof(IStatelessCellSerializable<T>.DeserializeFromCells),
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig);
            deserializationMethod.SetReturnType(typeof(T));
            deserializationMethod.SetParameters([typeof(ReadOnlySpan<DataCell>)]);
            if (typeof(T).IsValueType)
                MakeDeserializationValueTypeMethod(deserializationMethod.GetILGenerator(), properties);
            else
                MakeDeserializationMethod(deserializationMethod.GetILGenerator(), properties);
        }
        var dynamicType = typeBuilder.CreateType();
        var obj = Activator.CreateInstance(dynamicType) ?? throw new NullReferenceException();
        return (IStatelessCellSerializable<T>)obj;
    }

    private static void FieldResolve(ILGenerator ilGenerator, PropertyInfo pi)
    {
        switch (Type.GetTypeCode(pi.PropertyType))
        {
            case TypeCode.Int32:
            {
                ilGenerator.Emit(OpCodes.Ldfld,
                    typeof(DataCell).GetField(nameof(DataCell.DWord)) ?? throw new UnreachableException());
                break;
            }
            case TypeCode.Int64:
            {
                ilGenerator.Emit(OpCodes.Ldfld,
                    typeof(DataCell).GetField(nameof(DataCell.QWord)) ?? throw new UnreachableException());
                break;
            }
            case TypeCode.Single:
            {
                ilGenerator.Emit(OpCodes.Ldfld,
                    typeof(DataCell).GetField(nameof(DataCell.Single)) ?? throw new UnreachableException());
                break;
            }
            case TypeCode.Double:
            {
                ilGenerator.Emit(OpCodes.Ldfld,
                    typeof(DataCell).GetField(nameof(DataCell.Double)) ?? throw new UnreachableException());
                break;
            }
            case TypeCode.String:
            {
                ilGenerator.Emit(OpCodes.Call,
                    typeof(DataCell).GetMethod(nameof(DataCell.GetString)) ?? throw new UnreachableException());
                break;
            }
            default:
            {
                if (pi.PropertyType == typeof(byte[]))
                {
                    ilGenerator.Emit(OpCodes.Call,
                        typeof(DataCell).GetMethod(nameof(DataCell.GetBytes)) ?? throw new UnreachableException());
                    break;
                }
                throw new NotSupportedException(pi.PropertyType.ToString());
            }
        }
    }

    private static void MakeDeserializationMethod(ILGenerator ilGenerator, PropertyInfo[] properties)
    {
        var localResult = ilGenerator.DeclareLocal(typeof(T));
        ilGenerator.Emit(OpCodes.Newobj, typeof(T).GetConstructor(Type.EmptyTypes) ?? throw new UnreachableException());
        ilGenerator.Emit(OpCodes.Stloc_S, localResult);
        var i = 0;
        foreach (var property in properties)
        {
            ilGenerator.Emit(OpCodes.Ldloc_S, localResult);
            ilGenerator.Emit(OpCodes.Ldarga_S, 1);
            ilGenerator.Emit(OpCodes.Ldc_I4, i++);
            ilGenerator.Emit(OpCodes.Call, FlexCompilerHelpers.ReadOnlySpanIndexer);
            FieldResolve(ilGenerator, property);
            var setter = property.SetMethod ?? throw new UnreachableException();
            if (setter.IsVirtual || setter.IsAbstract)
                ilGenerator.Emit(OpCodes.Callvirt, setter);
            else 
                ilGenerator.Emit(OpCodes.Call, setter);
        }
        
        ilGenerator.Emit(OpCodes.Ldloc_S, localResult);
        ilGenerator.Emit(OpCodes.Ret);
    }

    private static void MakeDeserializationValueTypeMethod(ILGenerator ilGenerator, PropertyInfo[] properties)
    {
        var localResult = ilGenerator.DeclareLocal(typeof(T));
        ilGenerator.Emit(OpCodes.Ldloca_S, localResult);
        ilGenerator.Emit(OpCodes.Initobj, typeof(T));
        var i = 0;
        foreach (var property in properties)
        {
            ilGenerator.Emit(OpCodes.Ldloca_S, localResult);
            ilGenerator.Emit(OpCodes.Ldarga_S, 1);
            ilGenerator.Emit(OpCodes.Ldc_I4, i++);
            ilGenerator.Emit(OpCodes.Call, FlexCompilerHelpers.ReadOnlySpanIndexer);
            FieldResolve(ilGenerator, property);
            ilGenerator.Emit(OpCodes.Call, property.SetMethod ?? throw new UnreachableException());
        }
        
        ilGenerator.Emit(OpCodes.Ldloc_S, localResult);
        ilGenerator.Emit(OpCodes.Ret);
    }

    private static void MakeSerializationMethod(ILGenerator ilGenerator, PropertyInfo[] properties)
    {
        Type[]? types = null;
        var i = 0;
        foreach (var property in properties)
        {
            ilGenerator.Emit(OpCodes.Ldarga_S, 1); // Load the span address (2nd argument)
            ilGenerator.Emit(OpCodes.Ldc_I4, i++);
            ilGenerator.Emit(OpCodes.Call, FlexCompilerHelpers.SpanIndexer);
            ilGenerator.Emit(OpCodes.Ldarg_S, 2); // Load the struct address (3rd argument)
            var getter = property.GetMethod ?? throw new UnreachableException();
            if (getter.IsVirtual || getter.IsAbstract)
                ilGenerator.Emit(OpCodes.Call, getter);
            else 
                ilGenerator.Emit(OpCodes.Callvirt, getter);
            types ??= new Type[1];
            types[0] = property.PropertyType;
            ilGenerator.Emit(OpCodes.Newobj,
                typeof(DataCell).GetConstructor(types) ??
                throw new NotSupportedException($"Constructor type not supported: {types[0]}"));
            ilGenerator.Emit(OpCodes.Stobj, typeof(DataCell));
        }
        ilGenerator.Emit(OpCodes.Ret);
    }

    private static void MakeSerializationValueTypeMethod(ILGenerator ilGenerator, PropertyInfo[] properties)
    {
        Type[]? types = null;
        var i = 0;
        foreach (var property in properties)
        {
            ilGenerator.Emit(OpCodes.Ldarga_S, 1); // Load the span address (2nd argument)
            ilGenerator.Emit(OpCodes.Ldc_I4, i++);
            ilGenerator.Emit(OpCodes.Call, FlexCompilerHelpers.SpanIndexer);
            ilGenerator.Emit(OpCodes.Ldarga_S, 2); // Load the struct address (3rd argument)
            ilGenerator.Emit(OpCodes.Call, property.GetMethod ?? throw new UnreachableException());
            types ??= new Type[1];
            types[0] = property.PropertyType;
            ilGenerator.Emit(OpCodes.Newobj,
                typeof(DataCell).GetConstructor(types) ??
                throw new NotSupportedException($"Constructor type not supported: {types[0]}"));
            ilGenerator.Emit(OpCodes.Stobj, typeof(DataCell));
        }
        ilGenerator.Emit(OpCodes.Ret);
    }

    public static readonly IStatelessCellSerializable<T> Compiled = CompileType();
}

public static class FlexCompiler
{
    public static IStatelessCellSerializable<T> GetCompiledSerializer<T>() => FlexCompiler<T>.Compiled;

    public static IStatelessCellSerializable<T> GetDefaultSerializer<T>() => !typeof(T).IsPublic
        ? FallbackCellSerializable<T>.Default
        : FlexCompiler<T>.Compiled;
}