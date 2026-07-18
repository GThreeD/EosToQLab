namespace EosToQLab.Core.Exceptions;

public abstract class EosToQLabException : Exception
{
    protected EosToQLabException(string code, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
    }

    public string Code { get; }
}
