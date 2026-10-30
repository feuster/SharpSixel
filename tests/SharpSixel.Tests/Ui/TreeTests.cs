using SharpSixel.Ui;

namespace SharpSixel.Tests.Ui;

public class TreeTests
{
    private static readonly WidgetEnvironment Plain = new WidgetEnvironment(false, useAnsi: false);
    private static readonly WidgetEnvironment AnsiAscii = new WidgetEnvironment(false);
    private static readonly WidgetEnvironment SixelEnv = new WidgetEnvironment(true, 10, 20);
    private static readonly SixelColor Guide = new SixelColor(128, 128, 128);

    /// <summary>Root, A (A1, A2), B.</summary>
    private static Tree Sample(string? title = "Root")
    {
        var tree = new Tree { Title = title };
        TreeNode a = tree.AddNode("A");
        a.AddNode("A1");
        a.AddNode("A2");
        tree.AddNode("B");
        return tree;
    }

    private static string[] Lines(string frame) => frame.Split('\n');

    [Fact]
    public void Ascii_DrawsTitleAndGuides() =>
        Assert.Equal(
            new[] { "Root", "+-- A", "|   +-- A1", "|   `-- A2", "`-- B" },
            Lines(Sample().Render(Plain)));

    [Fact]
    public void Ascii_WithoutTitle_TopLevelNodesStartAtLeftEdge() =>
        Assert.Equal(
            new[] { "A", "+-- A1", "`-- A2", "B" },
            Lines(Sample(null).Render(Plain)));

    [Fact]
    public void Ascii_DeepLevels_KeepOrEndVerticalGuides()
    {
        var tree = new Tree { Title = "Root" };
        TreeNode a = tree.AddNode("A");
        TreeNode a1 = a.AddNode("A1");
        a1.AddNode("A1a");
        tree.AddNode("B").AddNode("B1");
        Assert.Equal(
            new[] { "Root", "+-- A", "|   `-- A1", "|       `-- A1a", "`-- B", "    `-- B1" },
            Lines(tree.Render(Plain)));
    }

    [Fact]
    public void CollapsedNode_HidesChildrenAndShowsSuffix()
    {
        Tree tree = Sample();
        tree.Nodes[0].Expanded = false;
        Assert.Equal(new[] { "Root", "+-- A [+]", "`-- B" }, Lines(tree.Render(Plain)));

        tree.Style.CollapsedSuffix = " ...";
        Assert.Equal("+-- A ...", Lines(tree.Render(Plain))[1]);
        tree.Nodes[1].Expanded = false;
        Assert.Equal("`-- B", Lines(tree.Render(Plain))[2]);
    }

    [Fact]
    public void Ascii_CustomCharactersAreUsed()
    {
        Tree tree = Sample();
        tree.Style.AsciiVertical = ':';
        tree.Style.AsciiBranch = 'o';
        tree.Style.AsciiLastBranch = 'x';
        tree.Style.AsciiHorizontal = '=';
        Assert.Equal(new[] { "Root", "o== A", ":   o== A1", ":   x== A2", "x== B" }, Lines(tree.Render(Plain)));
    }

    [Fact]
    public void Ascii_ControlCharactersAreDrawnAsSpaces()
    {
        var tree = new Tree { Title = "a\nb" };
        tree.AddNode("x\ty");
        Assert.Equal(new[] { "a b", "`-- x y" }, Lines(tree.Render(Plain)));
    }

    [Fact]
    public void Ascii_WithAnsi_ColorsTitleAndGuidesOnlyIfSet()
    {
        Tree tree = Sample();
        string[] lines = Lines(tree.Render(AnsiAscii));
        Assert.Equal("\u001b[38;2;52;152;219mRoot\u001b[0m", lines[0]);
        Assert.Equal("\u001b[38;2;128;128;128m+-- \u001b[0mA", lines[1]);

        tree.Style.GuideColor = SixelColor.Transparent;
        tree.Style.TitleColor = SixelColor.Transparent;
        Assert.Equal(new[] { "Root", "+-- A", "|   +-- A1", "|   `-- A2", "`-- B" }, Lines(tree.Render(AnsiAscii)));
        Assert.DoesNotContain("\u001b", Sample().Render(Plain), StringComparison.Ordinal);
    }

    [Fact]
    public void GetRows_CountsTitleAndVisibleNodes()
    {
        Tree tree = Sample();
        Assert.Equal(5, tree.GetRows(Plain));
        Assert.Equal(5, tree.GetRows(SixelEnv));
        Assert.Equal(4, Sample(null).GetRows(Plain));
        tree.Nodes[0].Expanded = false;
        Assert.Equal(3, tree.GetRows(SixelEnv));
        Assert.Equal(1, new Tree().GetRows(Plain));
        Assert.Equal(1, new Tree().GetRows(SixelEnv));
        Assert.Equal(string.Empty, new Tree().Render(Plain));
    }

    [Fact]
    public void NodesAndText_CanBeChangedBetweenFrames()
    {
        Tree tree = Sample();
        tree.Nodes[1].Text = "C";
        tree.Nodes[0].Nodes[0].AddNode("deep");
        Assert.Equal(
            new[] { "Root", "+-- A", "|   +-- A1", "|   |   `-- deep", "|   `-- A2", "`-- C" },
            Lines(tree.Render(Plain)));
    }

    [Fact]
    public void InvalidValues_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => new Tree(null!));
        Assert.Throws<ArgumentNullException>(() => new TreeNode(null!));
        Assert.Throws<ArgumentNullException>(() => new TreeNode("x").Text = null!);
        Assert.Throws<ArgumentNullException>(() => new Tree().Nodes.Add(null!));
        Assert.Throws<ArgumentNullException>(() => new TreeNode("x").Nodes.Add(null!));
        Assert.Throws<ArgumentNullException>(() => new Tree().AddNode(null!));
        Assert.Throws<ArgumentNullException>(() => new TreeStyle().CollapsedSuffix = null!);
        Assert.Throws<ArgumentOutOfRangeException>(() => new TreeStyle().LineThickness = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => new TreeStyle().LineThickness = 5);
    }

    [Fact]
    public void Image_SizeFollowsDepthAndRows()
    {
        SixelImage image = Sample().CreateImage(SixelEnv);
        Assert.Equal(80, image.Width);
        Assert.Equal(96, image.Height);
        Assert.Equal(40, Sample(null).CreateImage(SixelEnv).Width);
    }

    [Fact]
    public void Image_DrawsBranchHooksAndContinuations()
    {
        SixelImage image = Sample().CreateImage(SixelEnv);
        Assert.Equal(Guide, image.GetPixel(5, 25));
        Assert.Equal(Guide, image.GetPixel(20, 29));
        Assert.Equal(0, image.GetPixel(20, 25).A);
        Assert.Equal(0, image.GetPixel(35, 29).A);
        Assert.Equal(Guide, image.GetPixel(5, 50));
        Assert.Equal(Guide, image.GetPixel(60, 48));
        Assert.Equal(Guide, image.GetPixel(5, 70));
        Assert.Equal(0, image.GetPixel(45, 30).A);
    }

    [Fact]
    public void Image_LastNodeEndsItsVerticalGuideAtTheHook()
    {
        SixelImage image = Sample().CreateImage(SixelEnv);
        Assert.Equal(Guide, image.GetPixel(45, 60));
        Assert.Equal(0, image.GetPixel(45, 72).A);
        Assert.Equal(Guide, image.GetPixel(5, 80));
        Assert.Equal(0, image.GetPixel(5, 92).A);
        Assert.Equal(0, image.GetPixel(0, 0).A);
    }

    [Fact]
    public void Image_ColorAndThicknessAreCustomizable()
    {
        Tree tree = Sample();
        tree.Style.GuideColor = new SixelColor(255, 0, 0);
        tree.Style.LineThickness = 3;
        SixelImage image = tree.CreateImage(SixelEnv);
        Assert.Equal(new SixelColor(255, 0, 0), image.GetPixel(4, 25));
        Assert.Equal(new SixelColor(255, 0, 0), image.GetPixel(6, 25));

        tree.Style.GuideColor = SixelColor.Transparent;
        Assert.Equal(0, tree.CreateImage(SixelEnv).GetPixel(5, 25).A);
    }

    [Fact]
    public void SixelFrame_ContainsImageEraseAndTextAtGuideOffsets()
    {
        string frame = Sample().Render(SixelEnv);
        Assert.StartsWith("\u001b7", frame, StringComparison.Ordinal);
        Assert.Contains("\u001bP", frame, StringComparison.Ordinal);
        Assert.Contains("\u001b[4B\u001b[K\u001b8", frame, StringComparison.Ordinal);
        Assert.Contains("\u001b[38;2;52;152;219mRoot\u001b[0m\u001b8", frame, StringComparison.Ordinal);
        Assert.Contains("\u001b[1B\u001b[4CA\u001b8", frame, StringComparison.Ordinal);
        Assert.Contains("\u001b[2B\u001b[8CA1\u001b8", frame, StringComparison.Ordinal);
        Assert.EndsWith("\u001b8", frame, StringComparison.Ordinal);
    }

    [Fact]
    public void SixelFrame_WithoutGuides_DrawsNoImage()
    {
        var tree = new Tree();
        tree.AddNode("A");
        tree.AddNode("B");
        string frame = tree.Render(SixelEnv);
        Assert.DoesNotContain("\u001bP", frame, StringComparison.Ordinal);
        Assert.Contains("\u001b[1BB\u001b8", frame, StringComparison.Ordinal);
    }
}
