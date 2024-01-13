namespace Astra.Common;

public static class CommunicationProtocol
{
    public const ulong ServerIdentification     = 0x415354525341UL;
    public const ulong HandshakeResponse        = 0x41524C414EUL;

    public const uint NoAuthentication          = 0x4E4FU;
    public const uint PasswordAuthentication    = 0x50415353U;
    public const uint PubKeyAuthentication      = 0x505542U;

    public const uint AllowedConnection         = 0x4F4BU;
    public const uint RejectedConnection        = 0x4E4148U;
}