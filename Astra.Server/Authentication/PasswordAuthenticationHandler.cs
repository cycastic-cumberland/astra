using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using Astra.Engine;

namespace Astra.Server.Authentication;

public class PasswordAuthenticationHandler(
    Hash256 hashedPassword,
    Func<string, Hash256> hasher,
    Func<Hash256, Hash256, bool> comparer,
    int timeout) : IAuthenticationHandler
{
    public async Task<IAuthenticationHandler.AuthenticationState> Authenticate(TcpClient client, CancellationToken cancellationToken = default)
    {
        var stream = client.GetStream();
        await stream.WriteValueAsync(CommunicationProtocol.PasswordAuthentication, token: cancellationToken);
        var timer = Stopwatch.StartNew();
        while (client.Available < sizeof(int))
        {
#if DEBUG
            await Task.Delay(100, cancellationToken);
#endif
            if (timer.ElapsedMilliseconds > timeout)
                return IAuthenticationHandler.AuthenticationState.Timeout;
        }

        var pwdSize = await stream.ReadIntAsync(cancellationToken);
        timer.Restart();
        while (client.Available < pwdSize)
        {
#if DEBUG
            await Task.Delay(100, cancellationToken);
#endif
            if (timer.ElapsedMilliseconds > timeout)
                return IAuthenticationHandler.AuthenticationState.Timeout;
        }

        var bytes = new byte[pwdSize];
        await stream.ReadExactlyAsync(bytes, cancellationToken);
        var pwd = Encoding.UTF8.GetString(bytes);
        var hashed = hasher(pwd);
        return comparer(hashed, hashedPassword) ? IAuthenticationHandler.AuthenticationState.AllowConnection : IAuthenticationHandler.AuthenticationState.RejectConnection;
    }

    public void Dispose()
    {
        
    }
}