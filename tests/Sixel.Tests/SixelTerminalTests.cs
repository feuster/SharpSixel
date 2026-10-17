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
    public void Query_ReportsPixelGeometryAndSixelSupport()
    {
        var output = new StringWriter();
        string reply = Esc + "[4;600;800t" + Esc + "[6;20;10t" + Esc + "[?62;4;22c";

        SixelTerminalInfo info = SixelTerminal.Query(output, Source(reply));

        Assert.True(info.SupportsSixel);
        Assert.Equal(800, info.TextAreaWidth);
        Assert.Equal(600, info.TextAreaHeight);
        Assert.Equal(10, info.CellWidth);
        Assert.Equal(20, info.CellHeight);
        Assert.True(info.HasTextAreaSize && info.HasCellSize);
        Assert.Equal(SixelTerminal.PixelSizeRequest + SixelTerminal.DeviceAttributesRequest, output.ToString());
    }

    [Fact]
    public void Query_TerminalIgnoringSizeRequests_AnswersOnlyDa1()
    {
        SixelTerminalInfo info = SixelTerminal.Query(new StringWriter(), Source(Esc + "[?1;2c"));

        Assert.False(info.SupportsSixel);
        Assert.False(info.HasTextAreaSize);
        Assert.False(info.HasCellSize);
    }

    [Fact]
    public void Query_OnlyTextAreaReported_LeavesCellSizeUnknown()
    {
        SixelTerminalInfo info = SixelTerminal.Query(
            new StringWriter(), Source("typed" + Esc + "[4;300;400t" + Esc + "[?62;4c"));

        Assert.True(info.SupportsSixel);
        Assert.True(info.HasTextAreaSize);
        Assert.False(info.HasCellSize);
    }

    [Fact]
    public void Query_NoAnswer_ReturnsEmptyInfo()
    {
        SixelTerminalInfo info = SixelTerminal.Query(new StringWriter(), () => -1);

        Assert.False(info.SupportsSixel);
        Assert.False(info.HasTextAreaSize);
    }

    [Fact]
    public void Query_MalformedSizeReports_AreIgnored()
    {
        string reply = Esc + "[4;x;800t" + Esc + "[4;600t" + Esc + "[9;1;1t" + Esc + "[?62;4c";

        SixelTerminalInfo info = SixelTerminal.Query(new StringWriter(), Source(reply));

        Assert.True(info.SupportsSixel);
        Assert.False(info.HasTextAreaSize);
        Assert.False(info.HasCellSize);
    }

    [Theory]
    [InlineData("[4;600;800t", 4, 600, 800)]
    [InlineData("[6;20;10t", 6, 20, 10)]
    public void TryParseSizeReport_ReadsHeightBeforeWidth(string body, int kind, int height, int width)
    {
        Assert.True(SixelTerminal.TryParseSizeReport(Esc + body, out int k, out int h, out int w));
        Assert.Equal((kind, height, width), (k, h, w));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("[4;600;800c")]
    [InlineData("[4;600;-800t")]
    [InlineData("4;600;800t")]
    public void TryParseSizeReport_InvalidInput_ReturnsFalse(string? body)
    {
        string? reply = body == null ? null : body.StartsWith('[') ? Esc + body : body;

        Assert.False(SixelTerminal.TryParseSizeReport(reply, out _, out _, out _));
    }

    [Fact]
    public void Query_NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => SixelTerminal.Query(null!, () => -1));
        Assert.Throws<ArgumentNullException>(() => SixelTerminal.Query(new StringWriter(), null!));
    }

    [Fact]
    public void Detect_NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => SixelTerminal.Detect(null!, () => -1));
        Assert.Throws<ArgumentNullException>(() => SixelTerminal.Detect(new StringWriter(), null!));
        Assert.Throws<ArgumentNullException>(() => SixelTerminal.ReadResponse(null!));
    }
}
