namespace EosToQLab.Tests.Infrastructure.Import.Csv;

public sealed class EosCsvObjectBinderTests
{
    [Fact]
    public void Finds_required_columns_using_primary_name_or_alias()
    {
        Assert.Equal(["TEXT"],
            EosCsvObjectBinder<BindingTarget>.FindMissingRequiredColumns(new Dictionary<string, int>()));
        Assert.Empty(EosCsvObjectBinder<BindingTarget>.FindMissingRequiredColumns(
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                { ["OLD_TEXT"] = 0 }));
    }

    [Fact]
    public void Binds_and_converts_supported_types()
    {
        var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["OLD_TEXT"] = 0, ["COUNT"] = 1, ["AMOUNT"] = 2, ["MODE"] = 3,
            ["OPTIONAL"] = 4, ["FLAG"] = 5, ["RAW"] = 6
        };
        var value = EosCsvObjectBinder<BindingTarget>.Bind(
            [" text ", "7", "1.25", "Second", "", "true", " raw "], columns, 5);

        Assert.Equal("text", value.Text);
        Assert.Equal(7, value.Count);
        Assert.Equal(1.25m, value.Amount);
        Assert.Equal(TestMode.Second, value.Mode);
        Assert.Null(value.Optional);
        Assert.True(value.Flag);
        Assert.Equal(" raw ", value.Raw);
    }

    [Fact]
    public void Missing_row_values_use_type_defaults()
    {
        var columns = new Dictionary<string, int> { ["TEXT"] = 4, ["COUNT"] = 5 };
        var value = EosCsvObjectBinder<BindingTarget>.Bind([], columns, 1);
        Assert.Null(value.Text);
        Assert.Equal(0, value.Count);
    }

    [Fact]
    public void Invalid_value_is_wrapped_with_column_context()
    {
        var exception = Assert.Throws<CsvValueConversionException>(() =>
            EosCsvObjectBinder<BindingTarget>.Bind(["bad"], new Dictionary<string, int> { ["COUNT"] = 0 }, 12));
        Assert.Contains("COUNT", exception.Message);
        Assert.Contains("12", exception.Message);
    }

    [Fact]
    public void Property_without_setter_is_rejected()
    {
        var exception = Assert.Throws<TypeInitializationException>(() =>
            EosCsvObjectBinder<ReadOnlyBindingTarget>
                .FindMissingRequiredColumns(new Dictionary<string, int>()));

        Assert.IsType<CsvColumnBindingException>(exception.InnerException);
    }

    private enum TestMode
    {
        First,
        Second
    }

    private sealed class BindingTarget
    {
        [EosCsvColumn("TEXT", Required = true, Aliases = ["OLD_TEXT"])]
        public string? Text { get; set; }

        [EosCsvColumn("COUNT")] public int Count { get; set; }
        [EosCsvColumn("AMOUNT")] public decimal Amount { get; set; }
        [EosCsvColumn("MODE")] public TestMode Mode { get; set; }
        [EosCsvColumn("OPTIONAL")] public int? Optional { get; set; }
        [EosCsvColumn("FLAG")] public bool Flag { get; set; }
        [EosCsvColumn("RAW", Trim = false)] public string? Raw { get; set; }
        public string? Ignored { get; set; }
    }

    private sealed class ReadOnlyBindingTarget
    {
        // ReSharper disable once UnusedMember.Local
#pragma warning disable CA1822
        [EosCsvColumn("VALUE")] public string Value => string.Empty;
#pragma warning restore CA1822
    }
}