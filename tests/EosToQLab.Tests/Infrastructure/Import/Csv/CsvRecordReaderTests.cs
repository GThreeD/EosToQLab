namespace EosToQLab.Tests.Infrastructure.Import.Csv;

public sealed class CsvRecordReaderTests
{
    [Fact]
    public void Parses_commas_quotes_escaped_quotes_and_line_endings()
    {
        var records = CsvRecordReader.Parse("A,B\r\n\"one,two\",\"say \"\"hi\"\"\"\nlast,");
        Assert.Equal(3, records.Count);
        Assert.Equal(["A", "B"], records[0]);
        Assert.Equal(["one,two", "say \"hi\""], records[1]);
        Assert.Equal(["last", ""], records[2]);
    }

    [Fact]
    public void Preserves_newline_inside_quoted_field()
    {
        var records = CsvRecordReader.Parse("\"line1\nline2\",x");
        Assert.Equal("line1\nline2", records[0][0]);
    }

    [Fact]
    public void Empty_text_returns_no_records()
    {
        Assert.Empty(CsvRecordReader.Parse(string.Empty));
    }

    [Fact]
    public void Throws_for_unterminated_quote()
    {
        Assert.Throws<CsvUnterminatedQuotedFieldException>(() => CsvRecordReader.Parse("\"open"));
    }
}