#define YIELD_ON_WAIT

using System.Diagnostics;
using System.Net.Sockets;
namespace Astra.Common;

public static class AsyncHelpers
{
    public static void NetworkSpinLock<T>(this TcpClient client, long amount, int timeout) where T : Exception, new()
    {
        var timer = ValueStopwatch.Create();
        while (client.Available < amount)
        {
            if (timer.Elapsed.Microseconds > timeout)
                throw new T();
        }
    }
    public static 
#if DEBUG
        async
#endif
        ValueTask WaitForData(this TcpClient client, long amount)
    {
        while (client.Available < amount)
        {
#if DEBUG
                await Task.Delay(100);
#elif YIELD_ON_WAIT
            Thread.Yield();
#endif
        }
#if !DEBUG
        return ValueTask.CompletedTask;
#endif
    } 
    
    private static 
#if DEBUG
        async
#endif
        ValueTask WaitForDataInternal<T>(TcpClient client, long amount, int timeout)
        where T : Exception, new()
    {
        var timer = ValueStopwatch.Create();
        while (client.Available < amount)
        {
#if DEBUG
                await Task.Delay(100);
#elif YIELD_ON_WAIT
            Thread.Yield();
#endif
            if (timer.ElapsedMilliseconds > timeout)
            {
                throw new T();
            }
        }
#if !DEBUG
        return ValueTask.CompletedTask;
#endif
    }

    public static ValueTask WaitForData<T>(this TcpClient client, long amount, int timeout) where T : Exception, new()
    {
        return WaitForDataInternal<T>(client, amount, timeout);
    }
    
}