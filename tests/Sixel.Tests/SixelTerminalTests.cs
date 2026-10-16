namespace Sixel.Tests;

public class SixelTerminalTests
{
    private const string Esc = "\u001b";

    private static Func<int> Source(string text)
    {
        int index = 0;
        return () => index < text.Length ? text[index++] : -1;
    }

    [Theory]
    [InlineData("[?62;4;22c", true)]
    [InlineData("[?64;1;2;4;6;9c", true)]
    [InlineData("[?63;1;2;3;4;7c", true)]
    [InlineData("[?62;4c", true)]
    [InlineData("[?64;1;2;6;9;15;18;21;22c", false)]
    [InlineData("[?1;2c", false)]
    [InlineData("[?62;40;14c", false)]
    [InlineData("[?4c", false)]
    [InlineData("[?62;4", false)]
    [InlineData("[62;4c", false)]
    public void IsSixelSupported_ParsesDeviceAttributes(string body, bool expected)
    {
        Assert.Equal(expected, SixelTerminal.IsSixelSupported(Esc + body));
    }

    [Fact]
    public void IsSixelSupported_EightBitCsi_IsRecognized()
    {
        Assert.True(SixelTerminal.IsSixelSupported("\u009b?62;4c"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("garbage")]
    public void IsSixelSupported_InvalidInput_ReturnsFalse(string? response)
    {
        Assert.False(SixelTerminal.IsSixelSupported(response));
    }

    [Fact]
    public void ReadResponse_SkipsTypedAheadInput()
    {
        string? response = SixelTerminal.ReadResponse(Source("ab\r" + Esc + "[?62;4c" + "tail"));

        Assert.Equal(Esc + "[?62;4c", response);
    }

    [Fact]
    public void ReadResponse_Timeout_ReturnsNull()
    {
        Assert.Null(SixelTerminal.ReadResponse(Source(Esc + "[?62;4")));
        Assert.Null(SixelTerminal.ReadResponse(() => -1));
    }

    [Fact]
    public void ReadResponse_OverlongResponse_ReturnsNull()
    {
        Assert.Null(SixelTerminal.ReadResponse(() => Esc[0]));
    }

    [Fact]
    public void Detect_WritesRequestAndEvaluatesAnswer()
    {
        var output = new StringWriter();

        bool supported = SixelTerminal.Detect(output, Source(Esc + "[?62;4;22c"));

        Assert.True(supported);
        Assert.Equal(Esc + "[c", output.ToString());
    }

    [Fact]
    public void Detect_NoAnswer_ReturnsFalse()
    {
        Assert.False(SixelTerminal.Detect(new StringWriter(), () => -1));
    }

    [Fact]
    public void Detect_NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => SixelTerminal.Detect(null!, () => -1));
        Assert.Throws<ArgumentNullException>(() => SixelTerminal.Detect(new StringWriter(), null!));
        Assert.Throws<ArgumentNullException>(() => SixelTerminal.ReadResponse(null!));
    }
}
