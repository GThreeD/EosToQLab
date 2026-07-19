namespace EosToQLab.Tests.Core.Exceptions;

public sealed class EosToQLabExceptionTests
{
    [Fact]
    public void Base_constructor_keeps_code_message_and_inner_exception()
    {
        var inner = new InvalidOperationException("inner");
        var exception = new TestException("CODE", "message", inner);

        Assert.Equal("CODE", exception.Code);
        Assert.Equal("message", exception.Message);
        Assert.Same(inner, exception.InnerException);
    }

    private sealed class TestException(string code, string message, Exception inner)
        : EosToQLabException(code, message, inner);
}