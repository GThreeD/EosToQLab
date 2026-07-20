namespace EosToQLab.Tests.Core.Import;

public sealed class IEosShowArchiveCompatibilityTests
{
    [Fact]
    public void Declares_a_format_and_version_coverage_query()
    {
        var method = Assert.Single(typeof(IEosShowArchiveCompatibility).GetMethods());

        Assert.Equal(nameof(IEosShowArchiveCompatibility.IsCovered), method.Name);
        Assert.Equal(typeof(bool), method.ReturnType);
        Assert.Equal(
            [typeof(string), typeof(string)],
            method.GetParameters().Select(parameter => parameter.ParameterType));
    }
}