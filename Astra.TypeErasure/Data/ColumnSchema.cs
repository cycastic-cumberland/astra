using Astra.Common.Data;

namespace Astra.TypeErasure.Data;

public record ColumnSchema
{
    public readonly DataType Type;
    public readonly string ColumnName;
    public readonly bool IsIndex;
    public readonly int Index;
    public readonly bool ShouldBeHashed;
    public readonly int BTreeDegree;

    public ColumnSchema(DataType type, string columnName, bool shouldBeHashed, bool indexed, int index, int degree)
    {
        Type = type;
        ColumnName = columnName;
        ShouldBeHashed = shouldBeHashed;
        IsIndex = indexed;
        Index = index;
        BTreeDegree = degree;
    }
}