using Astra.TypeErasure.Data;

namespace Astra.Engine.v2.Data;

public class DatastoreContext(ColumnSchema[] tableSchema)
{
    public readonly ColumnSchema[] TableSchema = tableSchema;
    private ulong _current;
    
    public ulong NewRowId()
    {
        return Interlocked.Increment(ref _current);
    }
}