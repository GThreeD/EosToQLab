namespace EosToQLab.Core.Exceptions;

public sealed class UnexpectedApplicationException : EosToQLabException
{
    public UnexpectedApplicationException(Exception innerException)
        : base(
            "UNEXPECTED_APPLICATION_ERROR",
            "An unexpected application error occurred. Review the inner exception for technical details.",
            innerException) { }
}
