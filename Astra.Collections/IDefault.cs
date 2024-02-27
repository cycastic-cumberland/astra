namespace Astra.Collections;

public interface IDefault<out TSelf>
{
    public static abstract TSelf Default { get; }
}