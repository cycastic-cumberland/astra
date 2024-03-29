using System.Net.Sockets;
using Astra.Common;
using Astra.Common.Protocols;
using Astra.Common.StreamUtils;
using Astra.Engine;

namespace Astra.Server.Authentication;

public class NoAuthenticationHandler : IAuthenticationHandler
{
    public async Task<IAuthenticationHandler.AuthenticationState> Authenticate(TcpClient client,
        CancellationToken cancellationToken = default)
    {
        await client.GetStream().WriteValueAsync(CommunicationProtocol.NoAuthentication, cancellationToken);
        return IAuthenticationHandler.AuthenticationState.AllowConnection;
    }

    public void Dispose()
    {
        
    }
}