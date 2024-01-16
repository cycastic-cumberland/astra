using System.Diagnostics;
using System.Net.Sockets;
using System.Security.Cryptography;
using Astra.Common;

namespace Astra.Server.Authentication;

public class SaltedPasswordAuthenticationHandler(
    byte[] rawPassword,
    Func<byte[], Hash256> hasher,
    Func<Hash256, Hash256, bool> comparer,
    int timeout) : IAuthenticationHandler
{
    public async Task<IAuthenticationHandler.AuthenticationState> Authenticate(TcpClient client, CancellationToken cancellationToken = default)
    {
        var stream = client.GetStream();
        await stream.WriteValueAsync(CommunicationProtocol.SaltedPasswordAuthentication, token: cancellationToken);
        var salt = new byte[CommonProtocol.SaltLength];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }
        await stream.WriteAsync(salt, cancellationToken);
        var timer = Stopwatch.StartNew();
        while (client.Available < Hash256.Size)
        {
#if DEBUG
            await Task.Delay(100, cancellationToken);
#endif
            if (timer.ElapsedMilliseconds > timeout)
                return IAuthenticationHandler.AuthenticationState.Timeout;
        }

        var bytes = new byte[Hash256.Size];
        await stream.ReadExactlyAsync(bytes, cancellationToken);
        var userHash = Hash256.CreateUnsafe(bytes);
        var correctHash = hasher(CommonProtocol.CombineSalt(rawPassword, salt));
        return comparer(userHash, correctHash) ? IAuthenticationHandler.AuthenticationState.AllowConnection : IAuthenticationHandler.AuthenticationState.RejectConnection;
    }

    public void Dispose()
    {
        
    }
}