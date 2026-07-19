namespace EosToQLab.Tests.Infrastructure.QLab.Osc;

public sealed class SlipStreamReaderTests
{
    [Fact]
    public async Task Reads_consecutive_buffered_frames_and_unescapes_payloads()
    {
        byte[] first = [1, SlipCodec.End, SlipCodec.Escape, 3];
        byte[] second = [4, 5];
        await using var stream = new MemoryStream(SlipCodec.Frame(first).Concat(SlipCodec.Frame(second)).ToArray());
        var reader = new SlipStreamReader(stream, 3);
        Assert.Equal(first, await reader.ReadFrameAsync(CancellationToken.None));
        Assert.Equal(second, await reader.ReadFrameAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Ignores_leading_empty_frames()
    {
        await using var stream = new MemoryStream([SlipCodec.End, SlipCodec.End, .. SlipCodec.Frame([7])]);
        Assert.Equal(new byte[] { 7 }, await new SlipStreamReader(stream).ReadFrameAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Rejects_invalid_escape_and_closed_stream()
    {
        await using var invalid = new MemoryStream([SlipCodec.End, SlipCodec.Escape, 0xAA, SlipCodec.End]);
        await Assert.ThrowsAsync<QLabSlipFrameException>(() =>
            new SlipStreamReader(invalid).ReadFrameAsync(CancellationToken.None));
        await using var empty = new MemoryStream();
        await Assert.ThrowsAsync<QLabConnectionClosedException>(() =>
            new SlipStreamReader(empty).ReadFrameAsync(CancellationToken.None));
    }

    [Fact]
    public void Constructor_validates_arguments()
    {
        Assert.Throws<ArgumentNullException>(() => new SlipStreamReader(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SlipStreamReader(Stream.Null, 0));
    }
}