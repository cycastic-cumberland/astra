#define YIELD_ON_WAIT

using System.Net.Sockets;
using Astra.Common.Data;

namespace Astra.Common;


public static class AsyncHelpers
{
    public static void NetworkSpinLock<T>(this TcpClient client, long amount, int timeout) where T : Exception, new()
    {
        var timer = ValueStopwatch.Create();
        while (client.Available < amount)
        {
            Thread.SpinWait(10);
            if (timer.Elapsed.Microseconds > timeout)
                throw new T();
        }
    }

    public static void NetworkSpinLock(this TcpClient client, long amount, int timeout)
    {
        client.NetworkSpinLock<TimeoutException>(amount, timeout);
    }
    
    public static void NetworkWait<T>(this TcpClient client, long amount, int timeout) where T : Exception, new()
    {
        var timer = ValueStopwatch.Create();
        while (client.Available < amount)
        {
            Thread.Yield();
            if (timer.Elapsed.Microseconds > timeout)
                throw new T();
        }
    }
    
    public static void NetworkWait(this TcpClient client, long amount, int timeout)
    {
        client.NetworkWait<TimeoutException>(amount, timeout);
    }
    
    private static 
#if DEBUG
        async
#endif
        ValueTask WaitForDataInternal<T>(TcpClient client, long amount, int timeout, CancellationToken cancellationToken)
        where T : Exception, new()
    {
        var timer = ValueStopwatch.Create();
        while (client.Available < amount && !cancellationToken.IsCancellationRequested)
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

    public static ValueTask WaitForDataAsync<T>(this TcpClient client, long amount, int timeout, CancellationToken cancellationToken = default) where T : Exception, new()
    {
        return WaitForDataInternal<T>(client, amount, timeout, cancellationToken);
    }

    public static async ValueTask WaitForDataAsync<T>(this TcpClient client, Memory<byte> cluster, int timeout, CancellationToken cancellationToken = default) where T : Exception, new()
    {
        const int frameSize = 128; // 1024-bit
        var stream = client.GetStream();
        var timer = ValueStopwatch.Create();
        var readAmount = 0;
        while (readAmount < cluster.Length)
        {
            var bytesLeft = cluster.Length - readAmount;
            var bytesReadThisFrame = bytesLeft < frameSize ? bytesLeft : frameSize;
            while (client.Available < bytesReadThisFrame)
            {
#if YIELD_ON_WAIT
                Thread.Yield();
#endif
                if (timer.ElapsedMilliseconds > timeout)
                {
                    throw new T();
                }
            }

            await stream.ReadExactlyAsync(cluster.Slice(readAmount, bytesReadThisFrame), cancellationToken);
            readAmount += bytesReadThisFrame;
        }
    }
}