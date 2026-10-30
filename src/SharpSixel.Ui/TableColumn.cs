using System;

namespace SharpSixel.Ui;

/// <summary>One column of a <see cref="Table"/>: header text, alignment and optional fixed width.</summary>
public sealed class TableColumn
{
    private string _header;
    private int? _width;

    /// <summary>Creates a column.</summary>
    /// <param name="header">Header text (control characters are drawn as spaces).</param>
    /// <param name="alignment">Alignment of header and cells.</param>
    /// <param name="width">Text width in cells; null fits the column to its widest text. Longer text is cut with an ellipsis.</param>
    public TableColumn(string header, TableAlignment alignment = TableAlignment.Left, int? width = null)
    {
        _header = header ?? throw new ArgumentNullException(nameof(header));
        Alignment = alignment;
        Width = width;
    }

    /// <summary>Header text.</summary>
    public string Header
    {
        get => _header;
        set => _header = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Alignment of header and cells.</summary>
    public TableAlignment Alignment { get; set; }

    /// <summary>Text width in cells (at least 1) or null to fit the widest text. Fixed widths keep the table size stable between frames.</summary>
    public int? Width
    {
        get => _width;
        set => _width = value.HasValue ? Guard.AtLeast(value.Value, 1, nameof(value)) : (int?)null;
    }
}
