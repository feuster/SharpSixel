using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SharpSixel.Ui;

/// <summary>
/// Several progress bars below each other, one per task (like the progress display of a multi-task job). The group aligns the bars
/// of its tasks: every caption is padded to the widest one. Each task is an ordinary <see cref="ProgressBar"/> that the caller
/// updates (<see cref="ProgressBar.Value"/>, <see cref="ProgressBar.IsIndeterminate"/>); <see cref="Advance"/> animates the
/// indeterminate ones. The rows are the sum of the rows of the tasks and must not grow beyond the rows reserved in a
/// <see cref="LiveRegion"/>.
/// </summary>
public sealed class ProgressGroup : Widget
{
    private static readonly WidgetEnvironment SixelProbe = new WidgetEnvironment(true);
    private static readonly WidgetEnvironment AsciiProbe = new WidgetEnvironment(false);
    private readonly Collection<ProgressBar> _tasks = new TaskCollection();

    /// <summary>Creates a group whose tasks use the default style.</summary>
    public ProgressGroup()
        : this(new ProgressBarStyle())
    {
    }

    /// <summary>Creates a group whose tasks added with <see cref="AddTask"/> share the given style.</summary>
    public ProgressGroup(ProgressBarStyle style)
    {
        Style = style ?? throw new ArgumentNullException(nameof(style));
    }

    /// <summary>Style of the tasks created by <see cref="AddTask"/>; may be changed between frames.</summary>
    public ProgressBarStyle Style { get; }

    /// <summary>The tasks from top to bottom (null entries are rejected).</summary>
    public IList<ProgressBar> Tasks => _tasks;

    /// <summary>Creates a task that uses the style of the group and adds it at the bottom.</summary>
    /// <param name="caption">Text before the bar.</param>
    /// <param name="maximum">Value that represents 100 % of the task. Default 100.</param>
    public ProgressBar AddTask(string caption, double maximum = 100)
    {
        var task = new ProgressBar(Style) { Caption = caption, Maximum = maximum };
        _tasks.Add(task);
        return task;
    }

    /// <summary>Adds an existing progress bar at the bottom.</summary>
    public void Add(ProgressBar task) => _tasks.Add(task);

    /// <summary>Moves the indeterminate tasks to the next animation step.</summary>
    public void Advance()
    {
        foreach (ProgressBar task in _tasks)
        {
            if (task.IsIndeterminate)
            {
                task.Advance();
            }
        }
    }

    /// <inheritdoc />
    protected override int SixelRows => Build().GetRows(SixelProbe);

    /// <inheritdoc />
    protected override int AsciiRows => Build().GetRows(AsciiProbe);

    /// <inheritdoc />
    protected override int MeasureColumns(WidgetEnvironment environment) => Build().GetColumns(environment);

    /// <inheritdoc />
    protected override string RenderAscii(WidgetEnvironment environment) => Build().Render(environment);

    /// <inheritdoc />
    protected override string RenderSixel(WidgetEnvironment environment) => Build().Render(environment);

    private VerticalLayout Build()
    {
        int captionWidth = 0;
        foreach (ProgressBar task in _tasks)
        {
            captionWidth = Math.Max(captionWidth, TextFit.Width(PromptRow.Clean(task.Caption)));
        }

        var layout = new VerticalLayout();
        foreach (ProgressBar task in _tasks)
        {
            task.CaptionPad = captionWidth;
            layout.Add(task);
        }

        return layout;
    }

    private sealed class TaskCollection : Collection<ProgressBar>
    {
        protected override void InsertItem(int index, ProgressBar item) => base.InsertItem(index, Check(item));

        protected override void SetItem(int index, ProgressBar item) => base.SetItem(index, Check(item));

        private static ProgressBar Check(ProgressBar item) => item ?? throw new ArgumentNullException(nameof(item));
    }
}
