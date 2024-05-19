namespace Astra.Client;

public static class Exceptions
{
    public class EndianModeNotSupportedException(string? msg = null) : NotSupportedException(msg);
    public class HandshakeFailedException(string? msg = null) : Exception(msg);
    public class VersionNotSupportedException(string? msg = null) : NotSupportedException(msg);
    public class AuthenticationMethodNotSupportedException(string? msg = null) : Exception(msg);
    public class AuthenticationInfoNotProvidedException(string? msg = null) : Exception(msg);
    public class AuthenticationAttemptRejectedException(string? msg = null) : Exception(msg);
    public class NotConnectedException(string? msg = null) : Exception(msg);
    public class FaultedRequestException(string? msg = null) : Exception(msg);
    public class ConcurrencyException(string? msg = null) : Exception(msg);
}