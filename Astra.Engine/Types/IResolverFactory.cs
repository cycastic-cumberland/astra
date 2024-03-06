using Astra.Engine.Resolvers;

namespace Astra.Engine.Types;

public interface IResolverFactory<out T> where T : IColumnResolver
{
    public static abstract T Create(string columnName, int offset, bool isHashed);
}

public interface IPeripheralResolverFactory<out T> where T : IColumnResolver
{
    public static abstract T Create(string columnName, int offset, int index, bool isHashed);
}
