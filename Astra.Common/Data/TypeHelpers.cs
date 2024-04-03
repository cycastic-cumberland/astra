using System.Reflection;

namespace Astra.Common.Data;

public static class TypeHelpers
{
    public static IEnumerable<PropertyInfo> ToAccessibleProperties<T>() => typeof(T).GetProperties()
        .Where(o => o.GetMethod != null && o.SetMethod != null && o.GetMethod.IsPublic && o.SetMethod.IsPublic);
}