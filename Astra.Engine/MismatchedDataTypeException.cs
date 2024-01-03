namespace Astra.Engine;

public class MismatchedDataTypeException(string? msg = null) : Exception(msg);

public static class MismatchedDataTypeChecker
{
    public static void CheckDataType(this Stream reader, DataType type)
    {
        if (reader.ReadUInt().AstraDataType() != type)
            throw new MismatchedDataTypeException();
    }
}
