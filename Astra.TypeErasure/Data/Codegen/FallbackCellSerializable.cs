using Astra.Common.Data;

namespace Astra.TypeErasure.Data.Codegen;

public class FallbackCellSerializable<T> : IStatelessCellSerializable<T>
{
    public static readonly FallbackCellSerializable<T> Default = new();
    public void SerializeToCells(Span<DataCell> cells, T data)
    {
        var boxed = (object)data!;
        var i = 0;
        foreach (var pi in TypeHelpers.ToAccessibleProperties<T>())
        {
            switch (Type.GetTypeCode(pi.PropertyType))
            {
                case TypeCode.Int32:
                {
                    cells[i++] = new((int)boxed);
                    break;
                }
                case TypeCode.Int64:
                {
                    cells[i++] = new((long)boxed);
                    break;
                }
                case TypeCode.Single:
                {
                    cells[i++] = new((float)boxed);
                    break;
                }
                case TypeCode.Double:
                {
                    cells[i++] = new((double)boxed);
                    break;
                }
                case TypeCode.String:
                {
                    cells[i++] = new((string)boxed);
                    break;
                }
                default:
                {
                    if (pi.PropertyType == typeof(byte[]))
                    {
                        cells[i++] = new((byte[])boxed);
                        break;
                    }
                    throw new ArgumentOutOfRangeException(pi.PropertyType.Name);
                }
            }
        }
    }

    public T DeserializeFromCells(ReadOnlySpan<DataCell> cells)
    {
        var boxed = Activator.CreateInstance(typeof(T));
        var i = 0;
        foreach (var pi in TypeHelpers.ToAccessibleProperties<T>())
        {
            switch (Type.GetTypeCode(pi.PropertyType))
            {
                case TypeCode.Int32:
                {
                    pi.SetValue(boxed, cells[i++].DWord);
                    break;
                }
                case TypeCode.Int64:
                {
                    pi.SetValue(boxed, cells[i++].QWord);
                    break;
                }
                case TypeCode.Single:
                {
                    pi.SetValue(boxed, cells[i++].Single);
                    break;
                }
                case TypeCode.Double:
                {
                    pi.SetValue(boxed, cells[i++].Double);
                    break;
                }
                case TypeCode.String:
                {
                    pi.SetValue(boxed, cells[i++].GetString());
                    break;
                }
                default:
                {
                    if (pi.PropertyType == typeof(byte[]))
                    {
                        pi.SetValue(boxed, cells[i++].GetBytes());
                        break;
                    }
                    throw new ArgumentOutOfRangeException(pi.PropertyType.Name);
                }
            }
        }
        return (T)boxed!;
    }
}