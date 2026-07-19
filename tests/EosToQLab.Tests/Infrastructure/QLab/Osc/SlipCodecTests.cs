namespace EosToQLab.Tests.Infrastructure.QLab.Osc;

public sealed class SlipCodecTests
{
    [Fact]
    public void Frames_and_escapes_reserved_bytes()
    {
        var frame = SlipCodec.Frame([1, SlipCodec.End, 2, SlipCodec.Escape, 3]);
        Assert.Equal(new byte[]
        {
            SlipCodec.End, 1, SlipCodec.Escape, SlipCodec.EscapedEnd, 2,
            SlipCodec.Escape, SlipCodec.EscapedEscape, 3, SlipCodec.End, SlipCodec.End
        }, frame);
    }
}