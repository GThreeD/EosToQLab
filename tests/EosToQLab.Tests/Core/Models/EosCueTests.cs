using EosToQLab.Tests.TestDoubles;

namespace EosToQLab.Tests.Core.Models;

public sealed class EosCueTests
{
    [Theory]
    [InlineData(1, "2.5", "2.5")]
    [InlineData(3, "2.5", "3/2.5")]
    public void DisplayCueNumber_formats_list_one_and_other_lists(int listNumber, string cueNumber, string expected)
    {
        var cue = TestData.Cue(number: cueNumber, listNumber: listNumber);
        Assert.Equal(expected, cue.DisplayCueNumber);
    }

    [Theory]
    [InlineData("F1", true)]
    [InlineData(" f 1 ", true)]
    [InlineData("H1.5", true)]
    [InlineData("h", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("1", false)]
    public void HasFollowOrHang_recognizes_only_follow_and_hang_prefixes(string? value, bool expected)
    {
        Assert.Equal(expected, TestData.Cue(follow: value).HasFollowOrHang);
    }

    [Fact]
    public void Defaults_import_and_additional_values()
    {
        var cue = TestData.Cue();
        Assert.True(cue.ImportEnabled);
        Assert.Empty(cue.AdditionalValues);
    }
}