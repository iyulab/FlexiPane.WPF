using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FlexiPane.Serialization;

namespace FlexiPane.Controls;

/// <summary>
/// FlexiPanel partial class for layout serialization functionality
/// </summary>
public partial class FlexiPanel
{
    private static LayoutSerializer? _defaultSerializer;

    /// <summary>
    /// Gets or sets the default layout serializer
    /// </summary>
    public static LayoutSerializer DefaultSerializer
    {
        get => _defaultSerializer ??= new LayoutSerializer();
        set => _defaultSerializer = value;
    }

    /// <summary>
    /// Instance-specific serializer (uses DefaultSerializer if not set)
    /// </summary>
    public LayoutSerializer? LayoutSerializer { get; set; }

    /// <summary>
    /// Gets the active serializer (instance or default)
    /// </summary>
    private LayoutSerializer ActiveSerializer => LayoutSerializer ?? DefaultSerializer;

    #region Save Methods

    /// <summary>
    /// Saves the current layout to XML string
    /// </summary>
    public string SaveLayout()
    {
        return ActiveSerializer.SaveLayout(this);
    }

    /// <summary>
    /// Saves the current layout to a file
    /// </summary>
    public void SaveLayoutToFile(string filePath)
    {
        ActiveSerializer.SaveLayoutToFile(this, filePath);
    }

    /// <summary>
    /// Saves the current layout to a stream
    /// </summary>
    public void SaveLayoutToStream(Stream stream)
    {
        ActiveSerializer.SaveLayoutToStream(this, stream);
    }

    #endregion

    #region Load Methods

    /// <summary>
    /// Loads a layout from XML string
    /// </summary>
    /// <param name="xml">The XML layout string</param>
    /// <param name="contentCreator">Optional content creation delegate</param>
    public void LoadLayout(string xml, ContentCreationDelegate? contentCreator = null)
    {
        ActiveSerializer.LoadLayout(this, xml, contentCreator);
    }

    /// <summary>
    /// Loads a layout from a file
    /// </summary>
    /// <param name="filePath">Path to the layout file</param>
    /// <param name="contentCreator">Optional content creation delegate</param>
    public void LoadLayoutFromFile(string filePath, ContentCreationDelegate? contentCreator = null)
    {
        ActiveSerializer.LoadLayoutFromFile(this, filePath, contentCreator);
    }

    /// <summary>
    /// Loads a layout from a stream
    /// </summary>
    /// <param name="stream">Stream containing the layout</param>
    /// <param name="contentCreator">Optional content creation delegate</param>
    public void LoadLayoutFromStream(Stream stream, ContentCreationDelegate? contentCreator = null)
    {
        ActiveSerializer.LoadLayoutFromStream(this, stream, contentCreator);
    }

    #endregion

    #region Content Registration (Static)

    /// <summary>
    /// Registers a content factory globally
    /// </summary>
    public static void RegisterContentFactory(string contentKey, IContentFactory factory)
    {
        DefaultSerializer.RegisterContentFactory(contentKey, factory);
    }

    /// <summary>
    /// Registers a content creator delegate globally
    /// </summary>
    public static void RegisterContentCreator(string contentKey, ContentCreationDelegate creator)
    {
        DefaultSerializer.RegisterContentCreator(contentKey, creator);
    }

    /// <summary>
    /// Sets the default content creator globally
    /// </summary>
    public static void SetDefaultContentCreator(ContentCreationDelegate creator)
    {
        DefaultSerializer.SetDefaultContentCreator(creator);
    }

    #endregion

    #region Clear Methods

    /// <summary>
    /// Clears all panes and resets to initial state
    /// </summary>
    public void Clear()
    {
        // Clear the root content
        RootContent = null!;
        
        // Reset selected item
        SelectedItem = null;
        
        // Exit split mode
        IsSplitModeActive = false;
        
        // Create default initial pane
        var defaultPane = new FlexiPaneItem
        {
            Title = "Main Panel",
            CanSplit = true,
            Content = CreateClearedPaneContent()
        };
        
        RootContent = defaultPane;
        SelectedItem = defaultPane;
    }
    
    /// <summary>
    /// Creates default content for cleared panes
    /// </summary>
    private UIElement CreateClearedPaneContent()
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
            Padding = new Thickness(20)
        };
        
        var textBlock = new TextBlock
        {
            Text = "FlexiPane - Ready\n\nUse toolbar buttons or enable split mode to start.",
            FontSize = 14,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100))
        };
        
        border.Child = textBlock;
        return border;
    }

    #endregion
}