using System.Diagnostics;
using System.Net.Sockets;
using System.Security.Cryptography;
using Astra.Common;
using Astra.Engine;

namespace Astra.Server.Authentication;

public class PublicKeyAuthenticationHandler : IAuthenticationHandler
{
    private readonly byte[] _challenge = new byte[CommonProtocol.PublicKeyChallengeLength];
    private readonly RSA _rsa = new RSACryptoServiceProvider(2048);
    private readonly int _timeout;
    
    public PublicKeyAuthenticationHandler(string base64PubKey, int timeout)
    {
        _timeout = timeout;
        _rsa.ImportRSAPublicKey(Convert.FromBase64String(base64PubKey), out _);
    }
    
    public async Task<IAuthenticationHandler.AuthenticationState> Authenticate(TcpClient client, CancellationToken cancellationToken = default)
    {
        var stream = client.GetStream();
        await stream.WriteValueAsync(CommunicationProtocol.PubKeyAuthentication, token: cancellationToken);
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(_challenge);
        }

        await stream.WriteValueAsync(_challenge, cancellationToken);
        var timer = Stopwatch.StartNew();
        while (client.Available < sizeof(long))
        {
#if DEBUG
            await Task.Delay(100, cancellationToken);
#endif
            if (timer.ElapsedMilliseconds > _timeout)
                return IAuthenticationHandler.AuthenticationState.Timeout;
        }
        var privateKeySize = await stream.ReadLongAsync(cancellationToken);
        timer.Restart();
        while (client.Available < privateKeySize)
        {
#if DEBUG
            await Task.Delay(100, cancellationToken);
#endif
            if (timer.ElapsedMilliseconds > _timeout)
                return IAuthenticationHandler.AuthenticationState.Timeout;
        }
        timer.Stop();

        var signatureBytes = new byte[privateKeySize];
        await stream.ReadExactlyAsync(signatureBytes, cancellationToken);
        var verified = _rsa.VerifyData(_challenge, signatureBytes,
            HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
        return verified
            ? IAuthenticationHandler.AuthenticationState.AllowConnection
            : IAuthenticationHandler.AuthenticationState.RejectConnection;
    }

    public void Dispose()
    {
        _rsa.Dispose();
    }
}