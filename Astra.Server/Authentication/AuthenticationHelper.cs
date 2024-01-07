using Astra.Engine;

namespace Astra.Server.Authentication;

public static class AuthenticationHelper
{
    public static Func<IAuthenticationHandler> NoAuthentication() => () => new NoAuthenticationHandler();
    public static Func<IAuthenticationHandler> Sha256Authentication(Hash256 hashed, int timeout = 100_000) => 
        () => new PasswordAuthenticationHandler(hashed, Hash256.HashSha256, Hash256.Compare, timeout);
    public static Func<IAuthenticationHandler> RSA(string base64PublicKey, int timeout = 100_000) => () 
        => new PublicKeyAuthenticationHandler(base64PublicKey, timeout);
}