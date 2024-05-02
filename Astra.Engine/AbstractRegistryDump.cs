namespace Astra.Engine;

public abstract class AbstractRegistryDump
{
    public abstract Stream PrepareStream();
    public abstract void CloseStream(Stream stream);
    public abstract bool CanBeDumped { get; }

    private sealed class EmptyDump : AbstractRegistryDump
    {
        public override Stream PrepareStream()
        {
            throw new NotSupportedException();
        }

        public override void CloseStream(Stream stream)
        {
            throw new NotSupportedException();
        }

        public override bool CanBeDumped => false;
    }

    public static AbstractRegistryDump Empty => new EmptyDump();
}