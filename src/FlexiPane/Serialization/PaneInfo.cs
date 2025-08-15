using System.Windows;

namespace FlexiPane.Serialization;

/// <summary>
/// Information provided to content creators when loading layouts
/// </summary>
public class PaneInfo
{
    /// <summary>
    /// Unique identifier of the pane
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// User-defined content key for mapping
    /// </summary>
    public string? ContentKey { get; set; }

    /// <summary>
    /// Custom properties from the layout file
    /// </summary>
    public Dictionary<string, string> CustomProperties { get; set; }

    /// <summary>
    /// Position in the parent container (0 = first/left/top, 1 = second/right/bottom)
    /// </summary>
    public int PositionIndex { get; set; }

    /// <summary>
    /// Depth in the tree (0 = root)
    /// </summary>
    public int TreeDepth { get; set; }

    /// <summary>
    /// Path from root (e.g., "0.1.0" means first child -> second child -> first child)
    /// </summary>
    public string TreePath { get; set; }

    /// <summary>
    /// Parent container's orientation (if applicable)
    /// </summary>
    public SplitOrientation? ParentOrientation { get; set; }

    /// <summary>
    /// Size hints from the layout (if available)
    /// </summary>
    public Size? SizeHint { get; set; }

    public PaneInfo(string id)
    {
        Id = id;
        CustomProperties = new Dictionary<string, string>();
        TreePath = "0";
    }

    /// <summary>
    /// Creates a copy with updated tree position
    /// </summary>
    internal PaneInfo WithTreePosition(int positionIndex, int treeDepth, string treePath, SplitOrientation? parentOrientation)
    {
        return new PaneInfo(Id)
        {
            ContentKey = ContentKey,
            CustomProperties = new Dictionary<string, string>(CustomProperties),
            PositionIndex = positionIndex,
            TreeDepth = treeDepth,
            TreePath = treePath,
            ParentOrientation = parentOrientation,
            SizeHint = SizeHint
        };
    }
}