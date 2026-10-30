using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SharpSixel.Ui;

/// <summary>
/// One node of a <see cref="Tree"/>: a text line with optional child nodes. A node must not be added to itself or to one of
/// its own descendants.
/// </summary>
public sealed class TreeNode
{
    private readonly Collection<TreeNode> _nodes = new NodeCollection();
    private string _text;

    /// <summary>Creates a node.</summary>
    /// <param name="text">Node text, one terminal row (control characters are drawn as spaces).</param>
    public TreeNode(string text)
    {
        _text = text ?? throw new ArgumentNullException(nameof(text));
    }

    /// <summary>Node text.</summary>
    public string Text
    {
        get => _text;
        set => _text = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>True (default) to show the child nodes; false hides them and marks the node with <see cref="TreeStyle.CollapsedSuffix"/>.</summary>
    public bool Expanded { get; set; } = true;

    /// <summary>The child nodes (null nodes are rejected).</summary>
    public IList<TreeNode> Nodes => _nodes;

    /// <summary>Adds a child node and returns it.</summary>
    public TreeNode AddNode(string text)
    {
        var node = new TreeNode(text);
        _nodes.Add(node);
        return node;
    }

    internal sealed class NodeCollection : Collection<TreeNode>
    {
        protected override void InsertItem(int index, TreeNode item) => base.InsertItem(index, Check(item));

        protected override void SetItem(int index, TreeNode item) => base.SetItem(index, Check(item));

        private static TreeNode Check(TreeNode item) => item ?? throw new ArgumentNullException(nameof(item));
    }
}
