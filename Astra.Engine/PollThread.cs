namespace Astra.Engine;

public class PollThread : IDisposable
{
    private readonly Thread _server;
    private readonly Action _pollMethod;
    private readonly object _wakeLock;

    public object Wakelock => _wakeLock;

    public bool IsTerminated { get; private set; }

    private void Server()
    {
        while (!IsTerminated)
        {
            _pollMethod();
        }
    }
    
    public PollThread(Action pollMethod)
    {
        _pollMethod = pollMethod;
        _wakeLock = new();
        _server = new(Server);
        _server.Start();
    }

    public void Dispose()
    {
        IsTerminated = true;
        lock (_wakeLock)
        {
            Monitor.PulseAll(_wakeLock);
        }
        _server.Join();
    }
}