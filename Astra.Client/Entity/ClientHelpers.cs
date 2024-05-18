using Astra.Client.Simple;

namespace Astra.Client.Entity;

public static class ClientHelpers
{
    public static SimpleClientQueryable<T> AsQuery<T>(this AstraClient client)
    {
        return new(client);
    }

    public static async Task<List<T>> ToListAsync<T>(this Query<T> query, CancellationToken cancellationToken = default)
    {
        return new (await query.RunQueryAsync(cancellationToken));
    }

    public static async Task<T[]> ToArrayAsync<T>(this Query<T> query,
        CancellationToken cancellationToken = default)
    {
        return (await query.ToListAsync(cancellationToken)).ToArray();
    }
}