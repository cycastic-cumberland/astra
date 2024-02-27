namespace Astra.Engine.Types;

public static class TypeHandler
{
    public static readonly IReadOnlyDictionary<uint, ITypeHandler> Default = CreateDefault();

    public static void AddToMap<THandler, TDict>(this THandler handler, TDict dictionary)
        where THandler : ITypeHandler
        where TDict : IDictionary<uint, ITypeHandler>
    {
        dictionary[handler.FeaturingType] = handler;
    }

    public static Dictionary<uint, ITypeHandler> CreateDefault()
    {
        var map = new Dictionary<uint, ITypeHandler>();
        IntegerTypeHandler.Default.AddToMap(map);
        LongTypeHandler.Default.AddToMap(map);
        SingleTypeHandler.Default.AddToMap(map);
        DoubleTypeHandler.Default.AddToMap(map);
        StringTypeHandler.Default.AddToMap(map);
        BytesTypeHandler.Default.AddToMap(map);
        return map;
    }
}