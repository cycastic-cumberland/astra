using Astra.Common.Data;

namespace Astra.Engine.v2.Data;

public record ColumnSchema
{
    public readonly DataType Type;
    public readonly string ColumnName;
    public readonly int Index;
    public readonly bool ShouldBeHashed;
    public readonly int BTreeDegree;

    public ColumnSchema(DataType type, string columnName, bool shouldBeHashed, int index, int degree)
    {
        Type = type;
        ColumnName = columnName;
        ShouldBeHashed = shouldBeHashed;
        Index = index;
        BTreeDegree = degree;
    }
}