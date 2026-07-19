namespace EosToQLab.Tests.TestDoubles;

internal static class ExceptionAssertions
{
    public static void HasDetails(EosToQLabException exception, string code, params string[] messageParts)
    {
        Assert.Equal(code, exception.Code);
        foreach (var part in messageParts) Assert.Contains(part, exception.Message, StringComparison.Ordinal);
    }
}