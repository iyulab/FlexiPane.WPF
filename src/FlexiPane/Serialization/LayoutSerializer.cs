using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;
using FlexiPane.Controls;

namespace FlexiPane.Serialization;

/// <summary>
/// Handles serialization and deserialization of FlexiPane layouts
/// </summary>
public class LayoutSerializer
{
    private readonly Dictionary<string, IContentFactory> _contentFactories;
    private readonly Dictionary<string, ContentCreationDelegate> _contentCreators;
    private ContentCreationDelegate? _defaultContentCreator;

    public LayoutSerializer()
    {
        _contentFactories = new Dictionary<string, IContentFactory>();
        _contentCreators = new Dictionary<string, ContentCreationDelegate>();
    }

    #region Content Registration

    /// <summary>
    /// Registers a content factory for a specific content key
    /// </summary>
    public void RegisterContentFactory(string contentKey, IContentFactory factory)
    {
        if (string.IsNullOrEmpty(contentKey))
            throw new ArgumentException("Content key cannot be null or empty", nameof(contentKey));
        if (factory == null)
            throw new ArgumentNullException(nameof(factory));

        _contentFactories[contentKey] = factory;
    }

    /// <summary>
    /// Registers a simple content creator delegate for a specific content key
    /// </summary>
    public void RegisterContentCreator(string contentKey, ContentCreationDelegate creator)
    {
        if (string.IsNullOrEmpty(contentKey))
            throw new ArgumentException("Content key cannot be null or empty", nameof(contentKey));
        if (creator == null)
            throw new ArgumentNullException(nameof(creator));

        _contentCreators[contentKey] = creator;
    }

    /// <summary>
    /// Sets the default content creator for panes without a specific content key
    /// </summary>
    public void SetDefaultContentCreator(ContentCreationDelegate creator)
    {
        _defaultContentCreator = creator;
    }

    #endregion

    #region Serialization

    /// <summary>
    /// Saves the current layout of a FlexiPanel to XML
    /// </summary>
    public string SaveLayout(FlexiPanel panel)
    {
        if (panel == null)
            throw new ArgumentNullException(nameof(panel));

        var layout = CreateLayoutFromPanel(panel);
        return SerializeToXml(layout);
    }

    /// <summary>
    /// Saves the current layout of a FlexiPanel to a file
    /// </summary>
    public void SaveLayoutToFile(FlexiPanel panel, string filePath)
    {
        var xml = SaveLayout(panel);
        File.WriteAllText(filePath, xml, Encoding.UTF8);
    }

    /// <summary>
    /// Saves the current layout of a FlexiPanel to a stream
    /// </summary>
    public void SaveLayoutToStream(FlexiPanel panel, Stream stream)
    {
        var xml = SaveLayout(panel);
        using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
        writer.Write(xml);
    }

    #endregion

    #region Deserialization

    /// <summary>
    /// Loads a layout from XML and applies it to a FlexiPanel
    /// </summary>
    public void LoadLayout(FlexiPanel panel, string xml, ContentCreationDelegate? contentCreator = null)
    {
        if (panel == null)
            throw new ArgumentNullException(nameof(panel));
        if (string.IsNullOrEmpty(xml))
            throw new ArgumentException("XML cannot be null or empty", nameof(xml));

        var layout = DeserializeFromXml(xml);
        ApplyLayoutToPanel(panel, layout, contentCreator);
    }

    /// <summary>
    /// Loads a layout from a file and applies it to a FlexiPanel
    /// </summary>
    public void LoadLayoutFromFile(FlexiPanel panel, string filePath, ContentCreationDelegate? contentCreator = null)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Layout file not found", filePath);

        var xml = File.ReadAllText(filePath, Encoding.UTF8);
        LoadLayout(panel, xml, contentCreator);
    }

    /// <summary>
    /// Loads a layout from a stream and applies it to a FlexiPanel
    /// </summary>
    public void LoadLayoutFromStream(FlexiPanel panel, Stream stream, ContentCreationDelegate? contentCreator = null)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        var xml = reader.ReadToEnd();
        LoadLayout(panel, xml, contentCreator);
    }

    #endregion

    #region Private Methods

    private FlexiPaneLayout CreateLayoutFromPanel(FlexiPanel panel)
    {
        var layout = new FlexiPaneLayout
        {
            Name = panel.Name,
            WindowWidth = panel.ActualWidth,
            WindowHeight = panel.ActualHeight
        };

        if (panel.RootContent != null)
        {
            layout.RootNode = CreateNodeFromElement(panel.RootContent);
        }

        return layout;
    }

    private LayoutNode? CreateNodeFromElement(UIElement element)
    {
        if (element is FlexiPaneItem paneItem)
        {
            var node = LayoutNode.CreatePane();
            
            // Try to get content key from attached property or tag
            if (paneItem.Tag is string contentKey)
            {
                node.ContentKey = contentKey;
            }

            // Store custom properties if needed
            node.CustomProperties = ExtractCustomProperties(paneItem);

            return node;
        }
        else if (element is FlexiPaneContainer container)
        {
            if (container.FirstChild == null || container.SecondChild == null)
                return null;

            var firstNode = CreateNodeFromElement(container.FirstChild);
            var secondNode = CreateNodeFromElement(container.SecondChild);

            if (firstNode == null || secondNode == null)
                return null;

            var orientation = container.IsVerticalSplit
                ? SplitOrientation.Vertical
                : SplitOrientation.Horizontal;

            return LayoutNode.CreateContainer(orientation, container.SplitRatio, firstNode, secondNode);
        }

        return null;
    }

    private Dictionary<string, string>? ExtractCustomProperties(FlexiPaneItem paneItem)
    {
        // This can be extended to extract custom attached properties or other metadata
        var props = new Dictionary<string, string>();
        
        // Example: Store the type name of the content
        if (paneItem.Content != null)
        {
            props["ContentType"] = paneItem.Content.GetType().Name;
        }

        return props.Count > 0 ? props : null;
    }

    private void ApplyLayoutToPanel(FlexiPanel panel, FlexiPaneLayout layout, ContentCreationDelegate? contentCreator)
    {
        if (!layout.Validate(out string? error))
        {
            throw new InvalidOperationException($"Invalid layout: {error}");
        }

        if (layout.RootNode == null)
            return;

        // Clear existing content
        panel.RootContent = null!;

        // Rebuild the tree from the layout
        var rootElement = CreateElementFromNode(layout.RootNode, contentCreator, 0, "0");
        if (rootElement != null)
        {
            panel.RootContent = rootElement;
        }
    }

    private UIElement? CreateElementFromNode(LayoutNode node, ContentCreationDelegate? customCreator, 
        int depth, string path)
    {
        if (node.NodeType == LayoutNodeType.Pane)
        {
            var paneInfo = new PaneInfo(node.Id ?? Guid.NewGuid().ToString())
            {
                ContentKey = node.ContentKey,
                CustomProperties = node.CustomProperties ?? new Dictionary<string, string>(),
                TreeDepth = depth,
                TreePath = path
            };

            if (node.Width.HasValue && node.Height.HasValue)
            {
                paneInfo.SizeHint = new Size(node.Width.Value, node.Height.Value);
            }

            var content = CreateContentForPane(paneInfo, customCreator);
            
            var paneItem = new FlexiPaneItem
            {
                Content = content,
                Tag = node.ContentKey // Store for future serialization
            };

            return paneItem;
        }
        else if (node.NodeType == LayoutNodeType.Container)
        {
            if (node.FirstChild == null || node.SecondChild == null)
                return null;

            var firstElement = CreateElementFromNode(node.FirstChild, customCreator, 
                depth + 1, $"{path}.0");
            var secondElement = CreateElementFromNode(node.SecondChild, customCreator, 
                depth + 1, $"{path}.1");

            if (firstElement == null || secondElement == null)
                return null;

            var container = new FlexiPaneContainer
            {
                IsVerticalSplit = node.Orientation == SplitOrientation.Vertical,
                SplitRatio = node.SplitRatio ?? 0.5,
                FirstChild = firstElement,
                SecondChild = secondElement
            };

            return container;
        }

        return null;
    }

    private UIElement CreateContentForPane(PaneInfo paneInfo, ContentCreationDelegate? customCreator)
    {
        // Try custom creator first
        if (customCreator != null)
        {
            var content = customCreator(paneInfo);
            if (content != null)
                return content;
        }

        // Try registered factory for content key
        if (!string.IsNullOrEmpty(paneInfo.ContentKey))
        {
            if (_contentFactories.TryGetValue(paneInfo.ContentKey, out var factory))
            {
                return factory.CreateContent(paneInfo);
            }

            if (_contentCreators.TryGetValue(paneInfo.ContentKey, out var creator))
            {
                return creator(paneInfo);
            }
        }

        // Use default creator
        if (_defaultContentCreator != null)
        {
            return _defaultContentCreator(paneInfo);
        }

        // Fallback to empty content
#if DEBUG
        Debug.WriteLine($"[LayoutSerializer] No content creator found for pane {paneInfo.Id} with key '{paneInfo.ContentKey}'");
#endif
        return new System.Windows.Controls.TextBlock
        {
            Text = $"Pane: {paneInfo.Id}\nKey: {paneInfo.ContentKey ?? "(none)"}",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private string SerializeToXml(FlexiPaneLayout layout)
    {
        var serializer = new XmlSerializer(typeof(FlexiPaneLayout));
        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            Encoding = Encoding.UTF8,
            OmitXmlDeclaration = false
        };

        using var stringWriter = new StringWriter();
        using var xmlWriter = XmlWriter.Create(stringWriter, settings);
        serializer.Serialize(xmlWriter, layout);
        return stringWriter.ToString();
    }

    private FlexiPaneLayout DeserializeFromXml(string xml)
    {
        var serializer = new XmlSerializer(typeof(FlexiPaneLayout));
        using var stringReader = new StringReader(xml);
        using var xmlReader = XmlReader.Create(stringReader);
        
        var layout = serializer.Deserialize(xmlReader) as FlexiPaneLayout;
        if (layout == null)
            throw new InvalidOperationException("Failed to deserialize layout");

        return layout;
    }

    #endregion
}