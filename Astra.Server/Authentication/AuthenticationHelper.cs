using System.Text;
using Astra.Common;
using Astra.Engine;

namespace Astra.Server.Authentication;

public static class AuthenticationHelper
{
    public static Func<IAuthenticationHandler> NoAuthentication() => () => new NoAuthenticationHandler();
    public static Func<IAuthenticationHandler> SaltedSha256Authentication(string password, int timeout = 100_000)
    {
        var raw = Encoding.UTF8.GetBytes(password);
        return () => new SaltedPasswordAuthenticationHandler(raw, Hash256.HashSha256, Hash256.Compare, timeout);
    }
    public static Func<IAuthenticationHandler> RSA(string base64PublicKey, int timeout = 100_000) => () 
        => new PublicKeyAuthenticationHandler(base64PublicKey, timeout);
}