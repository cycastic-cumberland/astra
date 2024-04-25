using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using Astra.Common.Data;
using Astra.Common.StreamUtils;

namespace Astra.Common.Serializable;


file static class DynamicSerializableHelpers
{
    private static readonly AssemblyName DynamicSerializableAssemblyName = new("DynamicSerializableAssembly");

    private static readonly AssemblyBuilder DynamicSerializableAssemblyBuilder =
        AssemblyBuilder.DefineDynamicAssembly(DynamicSerializableAssemblyName, AssemblyBuilderAccess.Run);
    
    private static readonly ModuleBuilder DynamicSerializableModuleBuilder = 
        DynamicSerializableAssemblyBuilder.DefineDynamicModule("DynamicSerializableModule");

    public static TypeBuilder CreateType(Type type)
    {
        var timestamp = unchecked((ulong)Stopwatch.GetTimestamp());
        var name = $"dat_{timestamp}_{type.Name}";
        
        var builder = DynamicSerializableModuleBuilder.DefineType(name,
            TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed);

        var interfaceType = typeof(IStatelessSerializable<>).MakeGenericType(type);
        builder.AddInterfaceImplementation(interfaceType);
        
        return builder;
    }

    public static readonly string[] TypeParameterNames = ["TStream"];
    public static readonly Type[] GenericConstraint = [typeof(IStreamWrapper)];

    public static readonly ImmutableDictionary<Type, string> Readers =
        new Dictionary<Type, string>
        {
            [typeof(byte)] = nameof(IStreamWrapper.LoadByte),
            [typeof(int)] = nameof(IStreamWrapper.LoadInt),
            [typeof(uint)] = nameof(IStreamWrapper.LoadUInt),
            [typeof(long)] = nameof(IStreamWrapper.LoadLong),
            [typeof(ulong)] = nameof(IStreamWrapper.LoadULong),
            [typeof(float)] = nameof(IStreamWrapper.LoadSingle),
            [typeof(double)] = nameof(IStreamWrapper.LoadDouble),
            [typeof(string)] = nameof(IStreamWrapper.LoadString),
            [typeof(byte[])] = nameof(IStreamWrapper.LoadBytes),
        }.ToImmutableDictionary();
}

file static class DynamicSerializableHelpers<T>
{
    private static void MakeSerializationValueTypeMethod(ILGenerator ilGenerator, PropertyInfo[] properties, Type streamType)
    {
        var localStream = ilGenerator.DeclareLocal(streamType);
        Type[]? types = null;
        foreach (var property in properties)
        {
            var label = ilGenerator.DefineLabel();
            ilGenerator.Emit(OpCodes.Ldarga_S, 1);
            ilGenerator.Emit(OpCodes.Ldloca_S, localStream);
            ilGenerator.Emit(OpCodes.Initobj, streamType);
            ilGenerator.Emit(OpCodes.Ldloc_S, localStream);
            ilGenerator.Emit(OpCodes.Box, streamType);
            ilGenerator.Emit(OpCodes.Brtrue_S, label);
            
            ilGenerator.Emit(OpCodes.Ldobj, streamType);
            ilGenerator.Emit(OpCodes.Stloc_S, localStream);
            ilGenerator.Emit(OpCodes.Ldloca_S, localStream);
            
            ilGenerator.MarkLabel(label);
            ilGenerator.Emit(OpCodes.Ldarga_S, 2);
            ilGenerator.Emit(OpCodes.Call, property.GetMethod ?? throw new UnreachableException());
            ilGenerator.Emit(OpCodes.Constrained, streamType);
            types ??= new Type[1];
            types[0] = property.PropertyType;
            var mi = typeof(IStreamWrapper).GetMethod(nameof(IStreamWrapper.SaveValue), types) ??
                     throw new NotSupportedException($"Type not supported: {property.PropertyType.Name}");
            ilGenerator.Emit(OpCodes.Callvirt, mi);
        }
        ilGenerator.Emit(OpCodes.Ret);
    }
    
    private static void MakeSerializationMethod(ILGenerator ilGenerator, PropertyInfo[] properties,
        Type streamType)
    {
        var localStream = ilGenerator.DeclareLocal(streamType);
        Type[]? types = null;
        foreach (var property in properties)
        {
            var label = ilGenerator.DefineLabel();
            ilGenerator.Emit(OpCodes.Ldarga_S, 1);
            ilGenerator.Emit(OpCodes.Ldloca_S, localStream);
            ilGenerator.Emit(OpCodes.Initobj, streamType);
            ilGenerator.Emit(OpCodes.Ldloc_S, localStream);
            ilGenerator.Emit(OpCodes.Box, streamType);
            ilGenerator.Emit(OpCodes.Brtrue_S, label);
            
            ilGenerator.Emit(OpCodes.Ldobj, streamType);
            ilGenerator.Emit(OpCodes.Stloc_S, localStream);
            ilGenerator.Emit(OpCodes.Ldloca_S, localStream);
            
            ilGenerator.MarkLabel(label);
            ilGenerator.Emit(OpCodes.Ldarg_S, 2);
            var getter = property.GetMethod ?? throw new UnreachableException();
            if (getter.IsVirtual || getter.IsAbstract)
                ilGenerator.Emit(OpCodes.Call, getter);
            else 
                ilGenerator.Emit(OpCodes.Callvirt, getter);
            ilGenerator.Emit(OpCodes.Constrained, streamType);
            types ??= new Type[1];
            types[0] = property.PropertyType;
            var mi = typeof(IStreamWrapper).GetMethod(nameof(IStreamWrapper.SaveValue), types) ??
                     throw new NotSupportedException($"Type not supported: {property.PropertyType.Name}");
            ilGenerator.Emit(OpCodes.Callvirt, mi);
        }
        ilGenerator.Emit(OpCodes.Ret);
    }
    
    private static void MakeDeserializationValueTypeMethod(ILGenerator ilGenerator, PropertyInfo[] properties, Type streamType)
    {
        var localValue = ilGenerator.DeclareLocal(typeof(T));
        
        // Construct the object
        ilGenerator.Emit(OpCodes.Ldloca_S, localValue);
        ilGenerator.Emit(OpCodes.Initobj, typeof(T));

        foreach (var property in properties)
        {
            ilGenerator.Emit(OpCodes.Ldloca_S, localValue);
            ilGenerator.Emit(OpCodes.Ldarga_S, 1);
            ilGenerator.Emit(OpCodes.Constrained, streamType);
            var reader = DynamicSerializableHelpers.Readers[property.PropertyType];
            var mi = typeof(IStreamWrapper).GetMethod(reader) ??
                     throw new NotSupportedException($"Type not supported: {property.PropertyType.Name}");
            ilGenerator.Emit(OpCodes.Callvirt, mi);
            ilGenerator.Emit(OpCodes.Call, property.SetMethod ?? throw new UnreachableException());
        }
        
        ilGenerator.Emit(OpCodes.Ldloc_S, localValue);
        ilGenerator.Emit(OpCodes.Ret);
    }

    private static void MakeDeserializationMethod(ILGenerator ilGenerator, PropertyInfo[] properties,
        Type streamType)
    {
        var localValue = ilGenerator.DeclareLocal(typeof(T));
        ilGenerator.Emit(OpCodes.Newobj, typeof(T).GetConstructor(Type.EmptyTypes) ?? throw new UnreachableException());
        ilGenerator.Emit(OpCodes.Stloc_S, localValue);
        foreach (var property in properties)
        {
            ilGenerator.Emit(OpCodes.Ldloc_S, localValue);
            ilGenerator.Emit(OpCodes.Ldarga_S, 1);
            ilGenerator.Emit(OpCodes.Constrained, streamType);
            var reader = DynamicSerializableHelpers.Readers[property.PropertyType];
            var mi = typeof(IStreamWrapper).GetMethod(reader) ??
                     throw new NotSupportedException($"Type not supported: {property.PropertyType.Name}");
            ilGenerator.Emit(OpCodes.Callvirt, mi);
            var setter = property.SetMethod ?? throw new UnreachableException();
            if (setter.IsVirtual || setter.IsAbstract)
                ilGenerator.Emit(OpCodes.Callvirt, setter);
            else 
                ilGenerator.Emit(OpCodes.Call, setter);
        }
        
        ilGenerator.Emit(OpCodes.Ldloc_S, localValue);
        ilGenerator.Emit(OpCodes.Ret);
    }
    
    private static IStatelessSerializable<T> BuildDynamicType()
    {
        if (!typeof(T).IsPublic)
        {
            throw new NotSupportedException("Generic type `T` must be public");
        }
        if (!typeof(T).IsValueType && typeof(T).GetConstructor(Type.EmptyTypes) == null) 
            throw new NotSupportedException($"`{typeof(T).Name}` must have a public parameterless constructor");
        var typeBuilder = DynamicSerializableHelpers.CreateType(typeof(T));
        var properties = TypeHelpers.ToAccessibleProperties<T>().ToArray();
        {
            var serializationMethod = typeBuilder.DefineMethod(nameof(IStatelessSerializable<int>.SerializeStream),
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig);
            var genericBuilder = serializationMethod.DefineGenericParameters(DynamicSerializableHelpers.TypeParameterNames)[0];
            genericBuilder.SetInterfaceConstraints(DynamicSerializableHelpers.GenericConstraint);
            
            serializationMethod.SetReturnType(typeof(void));
            serializationMethod.SetParameters([genericBuilder, typeof(T)]);
            if (typeof(T).IsValueType)
                MakeSerializationValueTypeMethod(serializationMethod.GetILGenerator(), properties, genericBuilder);
            else
                MakeSerializationMethod(serializationMethod.GetILGenerator(), properties, genericBuilder);
        }
        {
            var deserializationMethod = typeBuilder.DefineMethod(nameof(IStatelessSerializable<int>.DeserializeStream),
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig);
            var genericBuilder = deserializationMethod.DefineGenericParameters(DynamicSerializableHelpers.TypeParameterNames)[0];
            genericBuilder.SetInterfaceConstraints(DynamicSerializableHelpers.GenericConstraint);
            deserializationMethod.SetReturnType(typeof(T));
            deserializationMethod.SetParameters([genericBuilder]);
            if (typeof(T).IsValueType)
                MakeDeserializationValueTypeMethod(deserializationMethod.GetILGenerator(), properties, genericBuilder);
            else
                MakeDeserializationMethod(deserializationMethod.GetILGenerator(), properties, genericBuilder);
        }

        var dynamicType = typeBuilder.CreateType();
        var obj = Activator.CreateInstance(dynamicType) ?? throw new NullReferenceException();
        return (IStatelessSerializable<T>)obj;
    }

    public static readonly IStatelessSerializable<T> DynamicSerializer = BuildDynamicType();
    public static GenericStatelessSerializable<T> FallbackSerializer => GenericStatelessSerializable<T>.Default;

    public static IStatelessSerializable<T> DefaultSerializer => typeof(T).IsPublic
        ? DynamicSerializer
        : FallbackSerializer;
}

public static class DynamicSerializable
{
    public static GenericStatelessSerializable<T> GetFallbackSerializer<T>() => DynamicSerializableHelpers<T>.FallbackSerializer;
    public static IStatelessSerializable<T> GetDefaultSerializer<T>() => DynamicSerializableHelpers<T>.DefaultSerializer;
    public static IStatelessSerializable<T> GetDynamicSerializer<T>() => DynamicSerializableHelpers<T>.DynamicSerializer;
    public static void EnsureBuilt<T>() => GetDefaultSerializer<T>();
    

    public static void BuildSerializers(params Type[] types)
    {
        foreach (var type in types)
        {
            var handler = typeof(DynamicSerializableHelpers<>).MakeGenericType(type);
            _ = handler.GetField(nameof(DynamicSerializableHelpers<int>.DefaultSerializer))?.GetValue(null);
        }
    }
}