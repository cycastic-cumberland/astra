using Astra.Common.Data;
using Astra.Common.StreamUtils;

namespace Astra.Common.Serializable;

public sealed class GenericStatelessStreamSerializable<T> : IStatelessStreamSerializable<T>
{
    public void SerializeStream<TStream>(TStream writer, T value) where TStream : IStreamWrapper
    {
        var boxed = (object)value!;
        foreach (var pi in TypeHelpers.ToAccessibleProperties<T>())
        {
            switch (Type.GetTypeCode(pi.PropertyType))
            {
                case TypeCode.Byte:
                {
                    writer.SaveValue((byte)(pi.GetValue(boxed) ?? throw new NullReferenceException()));
                    break;
                }
                case TypeCode.Int32:
                {
                    writer.SaveValue((int)(pi.GetValue(boxed) ?? throw new NullReferenceException()));
                    break;
                }
                case TypeCode.UInt32:
                {
                    writer.SaveValue((uint)(pi.GetValue(boxed) ?? throw new NullReferenceException()));
                    break;
                }
                case TypeCode.Int64:
                {
                    writer.SaveValue((long)(pi.GetValue(boxed) ?? throw new NullReferenceException()));
                    break;
                }
                case TypeCode.UInt64:
                {
                    writer.SaveValue((ulong)(pi.GetValue(boxed) ?? throw new NullReferenceException()));
                    break;
                }
                case TypeCode.Single:
                {
                    writer.SaveValue((float)(pi.GetValue(boxed) ?? throw new NullReferenceException()));
                    break;
                }
                case TypeCode.Double:
                {
                    writer.SaveValue((double)(pi.GetValue(boxed) ?? throw new NullReferenceException()));
                    break;
                }
                default:
                {
                    if (pi.PropertyType == typeof(string))
                    {
                        writer.SaveValue((string)(pi.GetValue(boxed) ?? throw new NullReferenceException()));
                        break;
                    }
                    if (pi.PropertyType == typeof(byte[]))
                    {
                        writer.SaveValue((byte[])(pi.GetValue(boxed) ?? throw new NullReferenceException()));
                        break;
                    }
                    throw new ArgumentOutOfRangeException(pi.PropertyType.Name);
                }
            }
        }
    }

    public T DeserializeStream<TStream>(TStream reader) where TStream : IStreamWrapper
    {
        var boxed = Activator.CreateInstance(typeof(T));
        foreach (var pi in TypeHelpers.ToAccessibleProperties<T>())
        {
            switch (Type.GetTypeCode(pi.PropertyType))
            {
                case TypeCode.Byte:
                {
                    pi.SetValue(boxed, reader.LoadByte());
                    break;
                }
                case TypeCode.Int32:
                {
                    pi.SetValue(boxed, reader.LoadInt());
                    break;
                }
                case TypeCode.UInt32:
                {
                    pi.SetValue(boxed, reader.LoadUInt());
                    break;
                }
                case TypeCode.Int64:
                {
                    pi.SetValue(boxed, reader.LoadLong());
                    break;
                }
                case TypeCode.UInt64:
                {
                    pi.SetValue(boxed, reader.LoadULong());
                    break;
                }
                case TypeCode.Single:
                {
                    pi.SetValue(boxed, reader.LoadSingle());
                    break;
                }
                case TypeCode.Double:
                {
                    pi.SetValue(boxed, reader.LoadDouble());
                    break;
                }
                default:
                {
                    if (pi.PropertyType == typeof(string))
                    {
                        pi.SetValue(boxed, reader.LoadString());
                        break;
                    }
                    if (pi.PropertyType == typeof(byte[]))
                    {
                        pi.SetValue(boxed, reader.LoadBytes());
                        break;
                    }
                    throw new ArgumentOutOfRangeException(nameof(pi.PropertyType));
                }
            }
        }

        return (T)boxed!;
    }

    public static readonly GenericStatelessStreamSerializable<T> Default = new();
}