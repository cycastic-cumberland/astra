namespace Astra.Client.Simple.Aggregator;

internal static class ColumnHelper
{
    public static readonly IReadOnlyDictionary<Type, Func<int, object>> Lookup = new Dictionary<Type, Func<int, object>>
    {
        [typeof(int)] = CreateInt,
        [typeof(long)] = CreateLong,
        [typeof(float)] = CreateSingle,
        [typeof(double)] = CreateDouble,
        [typeof(string)] = CreateString,
        [typeof(byte[])] = CreateBytes,
    };

    private static IntegerColumn CreateInt(int offset) => new(offset);
    private static LongColumn CreateLong(int offset) => new(offset);
    private static SingleColumn CreateSingle(int offset) => new(offset);
    private static DoubleColumn CreateDouble(int offset) => new(offset);
    private static StringColumn CreateString(int offset) => new(offset);
    private static BytesColumn CreateBytes(int offset) => new(offset);
}

public static class AstraColumns<T>
{
    public static IAstraColumnQuery<T> Default(int offset) => (IAstraColumnQuery<T>)ColumnHelper.Lookup[typeof(T)](offset);
}

public class AstraTable<T1>
{
    public readonly IAstraColumnQuery<T1> Column1 = AstraColumns<T1>.Default(0);
}

public class AstraTable<T1, T2>
{
    public readonly IAstraColumnQuery<T1> Column1 = AstraColumns<T1>.Default(0);
    public readonly IAstraColumnQuery<T2> Column2 = AstraColumns<T2>.Default(1);
}

public class AstraTable<T1, T2, T3>
{
    public readonly IAstraColumnQuery<T1> Column1 = AstraColumns<T1>.Default(0);
    public readonly IAstraColumnQuery<T2> Column2 = AstraColumns<T2>.Default(1);
    public readonly IAstraColumnQuery<T3> Column3 = AstraColumns<T3>.Default(2);
}

public class AstraTable<T1, T2, T3, T4>
{
    public readonly IAstraColumnQuery<T1> Column1 = AstraColumns<T1>.Default(0);
    public readonly IAstraColumnQuery<T2> Column2 = AstraColumns<T2>.Default(1);
    public readonly IAstraColumnQuery<T3> Column3 = AstraColumns<T3>.Default(2);
    public readonly IAstraColumnQuery<T4> Column4 = AstraColumns<T4>.Default(3);
}

public class AstraTable<T1, T2, T3, T4, T5>
{
    public readonly IAstraColumnQuery<T1> Column1 = AstraColumns<T1>.Default(0);
    public readonly IAstraColumnQuery<T2> Column2 = AstraColumns<T2>.Default(1);
    public readonly IAstraColumnQuery<T3> Column3 = AstraColumns<T3>.Default(2);
    public readonly IAstraColumnQuery<T4> Column4 = AstraColumns<T4>.Default(3);
    public readonly IAstraColumnQuery<T5> Column5 = AstraColumns<T5>.Default(4);
}

public class AstraTable<T1, T2, T3, T4, T5, T6>
{
    public readonly IAstraColumnQuery<T1> Column1 = AstraColumns<T1>.Default(0);
    public readonly IAstraColumnQuery<T2> Column2 = AstraColumns<T2>.Default(1);
    public readonly IAstraColumnQuery<T3> Column3 = AstraColumns<T3>.Default(2);
    public readonly IAstraColumnQuery<T4> Column4 = AstraColumns<T4>.Default(3);
    public readonly IAstraColumnQuery<T5> Column5 = AstraColumns<T5>.Default(4);
    public readonly IAstraColumnQuery<T6> Column6 = AstraColumns<T6>.Default(5);
}