using System.Net.Sockets;

namespace Astra.Server.Authentication;


public interface IAuthenticationHandler : IDisposable
{
    public enum AuthenticationState
    {
        AllowConnection,
        RejectConnection,
        Timeout
    }
    public Task<AuthenticationState> Authenticate(TcpClient client, CancellationToken cancellationToken = default);
}