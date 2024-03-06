using System.Diagnostics;
using System.Net.Sockets;
using System.Security.Cryptography;
using Astra.Common;

namespace Astra.Server.Authentication;

public class SaltedPasswordAuthenticationHandler(
    byte[] rawPassword,
    Func<ReadOnlyMemory<byte>, Hash256> hasher,
    Func<Hash256, Hash256, bool> comparer,
    int timeout) : IAuthenticationHandler
{
    public async Task<IAuthenticationHandler.AuthenticationState> Authenticate(TcpClient client, CancellationToken cancellationToken = default)
    {
        var stream = client.GetStream();
        await stream.WriteValueAsync(CommunicationProtocol.SaltedPasswordAuthentication, token: cancellationToken);
        using var salt = BytesCluster.Rent(CommonProtocol.SaltLength);
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt.Writer);
        }
        await stream.WriteAsync(salt.ReaderMemory, cancellationToken);
        var timer = Stopwatch.StartNew();
        while (client.Available < Hash256.Size)
        {
#if DEBUG
            await Task.Delay(100, cancellationToken);
#else
            Thread.Yield();
#endif
            if (timer.ElapsedMilliseconds > timeout)
                return IAuthenticationHandler.AuthenticationState.Timeout;
        }

        using var bytes = BytesCluster.Rent(Hash256.Size);
        await stream.ReadExactlyAsync(bytes.WriterMemory, cancellationToken);
        var userHash = Hash256.Create(bytes.Reader);
        using var combined = CommonProtocol.CombineSalt(rawPassword, salt.Reader);
        var correctHash = hasher(combined.ReaderMemory);
        return comparer(userHash, correctHash) ? IAuthenticationHandler.AuthenticationState.AllowConnection : IAuthenticationHandler.AuthenticationState.RejectConnection;
    }

    public void Dispose()
    {
        
    }
}