using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FlexiPane.Commands;
using FlexiPane.Events;

namespace FlexiPane.Controls;

/// <summary>
/// Top-level container and global state manager for FlexiPane system
/// Uses WPF Attached Property pattern to inherit state to child elements
/// </summary>
[TemplatePart(Name = "PART_RootContent", Type = typeof(ContentPresenter))]
public partial class FlexiPanel : Control
{
    static FlexiPanel()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(FlexiPanel), 
            new FrameworkPropertyMetadata(typeof(FlexiPanel)));
        
        // Make FlexiPanel focusable to handle keyboard commands
        FocusableProperty.OverrideMetadata(typeof(FlexiPanel),
            new FrameworkPropertyMetadata(true));
    }
    
    public FlexiPanel()
    {
        // Initialize commands
        CloseSelectedPaneCommand = new RelayCommand(ExecuteCloseSelectedPane, CanExecuteCloseSelectedPane);
        
        // Add instance event handlers
        AddHandler(ContentRequestedEvent, new ContentRequestedEventHandler(OnContentRequested), true);
        AddHandler(PaneClosingEvent, new PaneClosingEventHandler(OnPaneClosing), true);
        
        // Also handle Loaded for additional initialization
        Loaded += OnLoaded;
        
        // Handle keyboard input for global commands
        PreviewKeyUp += OnPanelPreviewKeyUp;
    }

    private UIElement CreateSimpleInfoContent()
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
            Padding = new Thickness(20)
        };
        
        var textBlock = new TextBlock
        {
            Text = "FlexiPane - Dynamic Panel Splitter\n\nClick 'Toggle Split Mode' to activate splitting functionality.",
            FontSize = 14,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100))
        };
        
        border.Child = textBlock;
        return border;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Use Dispatcher to ensure bindings are fully established
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.DataBind, new Action(() =>
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[FlexiPanel] OnLoaded - RootContent is null: {RootContent == null}");
            System.Diagnostics.Debug.WriteLine($"[FlexiPanel] OnLoaded - IsSplitModeActive: {IsSplitModeActive}");
            if (RootContent != null)
            {
                System.Diagnostics.Debug.WriteLine($"[FlexiPanel] OnLoaded - Existing RootContent type: {RootContent.GetType().Name}");
            }
#endif
            
            // Try to get initial content from ContentRequested event now that handlers are connected
            if (IsDefaultContent(RootContent))
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FlexiPanel] OnLoaded - Requesting initial content via ContentRequested event");
#endif
                var contentEventArgs = new Events.ContentRequestedEventArgs("InitialContent")
                {
                    RoutedEvent = ContentRequestedEvent
                };
                
                // Raise the event to get content from the application  
                RaiseEvent(contentEventArgs);
                
                // Use requested content if provided
                if (contentEventArgs.RequestedContent is UIElement requestedContent)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[FlexiPanel] OnLoaded - Got content from event: {requestedContent.GetType().Name}");
#endif
                    
                    // FlexiPaneItem이 아니면 자동으로 래핑
                    if (requestedContent is FlexiPaneItem)
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine($"[FlexiPanel] OnLoaded - Content is already FlexiPaneItem, using directly");
#endif
                        RootContent = requestedContent;
                    }
                    else
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine($"[FlexiPanel] OnLoaded - Wrapping content in FlexiPaneItem for splitting capability");
#endif
                        var wrappedItem = new FlexiPaneItem
                        {
                            Title = "Main Panel",
                            CanSplit = IsSplitModeActive,
                            Content = requestedContent
                        };
                        // Split mode inherited through binding
                        RootContent = wrappedItem;
                    }
                }
                else
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[FlexiPanel] OnLoaded - No content provided from event, keeping default");
#endif
                }
            }
            
            // Apply current split mode state to existing content if any
            if (RootContent is FlexiPaneItem paneItem)
            {
                // Split mode is automatically inherited through WPF property inheritance
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FlexiPanel] OnLoaded - FlexiPaneItem found, split mode will be inherited");
#endif
                
                // Select this pane if none is selected
                if (SelectedItem == null)
                {
                    paneItem.IsSelected = true;
                }
            }
        }));
    }
    
    
    private UIElement CreateSamplePaneContent(string title, Color backgroundColor)
    {
        var border = new Border
        {
            Background = new SolidColorBrush(backgroundColor),
            Padding = new Thickness(20),
            CornerRadius = new CornerRadius(4)
        };
        
        var stackPanel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        
        var titleBlock = new TextBlock
        {
            Text = title,
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10)
        };
        
        var instructionBlock = new TextBlock
        {
            Text = "Right-click or use keyboard shortcuts to split this pane",
            FontSize = 12,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.8
        };
        
        stackPanel.Children.Add(titleBlock);
        stackPanel.Children.Add(instructionBlock);
        border.Child = stackPanel;
        
        return border;
    }
    
    /// <summary>
    /// Create auto-generated content for new panes when splitting
    /// </summary>
    private UIElement CreateAutoGeneratedPaneContent()
    {
        var random = new Random();
        var colors = new[] { Colors.CornflowerBlue, Colors.SeaGreen, Colors.Coral, Colors.MediumPurple, Colors.Gold, Colors.LightSeaGreen };
        var selectedColor = colors[random.Next(colors.Length)];
        var paneNumber = CountTotalPanes() + 1;
        
        return CreateSamplePaneContent($"Panel {paneNumber}", selectedColor);
    }

    #region Instance Properties

    /// <summary>
    /// Global split mode state - single source of truth
    /// </summary>
    public bool IsSplitModeActive
    {
        get { return (bool)GetValue(IsSplitModeActiveProperty); }
        set { SetValue(IsSplitModeActiveProperty, value); }
    }

    public static readonly DependencyProperty IsSplitModeActiveProperty =
        DependencyProperty.Register(nameof(IsSplitModeActive), typeof(bool), typeof(FlexiPanel),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits, OnIsSplitModeActiveChanged));

    private static void OnIsSplitModeActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FlexiPanel panel)
        {
            bool isActive = (bool)e.NewValue;
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[FlexiPanel] IsSplitModeActive changed: {isActive}");
#endif
            panel.OnSplitModeChangedInternal(isActive);
        }
    }


    /// <summary>
    /// Root content (FlexiPaneItem or FlexiPaneContainer)
    /// </summary>
    public UIElement RootContent
    {
        get { return (UIElement)GetValue(RootContentProperty); }
        set { SetValue(RootContentProperty, value); }
    }

    public static readonly DependencyProperty RootContentProperty =
        DependencyProperty.Register(nameof(RootContent), typeof(UIElement), typeof(FlexiPanel),
            new PropertyMetadata(null, OnRootContentChanged));

    /// <summary>
    /// Currently selected/focused pane item
    /// </summary>
    public FlexiPaneItem? SelectedItem
    {
        get { return (FlexiPaneItem?)GetValue(SelectedItemProperty); }
        private set { SetValue(SelectedItemProperty, value); }
    }

    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(nameof(SelectedItem), typeof(FlexiPaneItem), typeof(FlexiPanel),
            new PropertyMetadata(null, OnSelectedItemChanged));

    /// <summary>
    /// Command to close the currently selected pane
    /// </summary>
    public ICommand CloseSelectedPaneCommand
    {
        get { return (ICommand)GetValue(CloseSelectedPaneCommandProperty); }
        set { SetValue(CloseSelectedPaneCommandProperty, value); }
    }

    public static readonly DependencyProperty CloseSelectedPaneCommandProperty =
        DependencyProperty.Register(nameof(CloseSelectedPaneCommand), typeof(ICommand), typeof(FlexiPanel),
            new PropertyMetadata(null));

    #endregion

    #region Internal Property Handlers

    private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FlexiPanel panel)
        {
            // Update Command CanExecute state when selected item changes
            if (panel.CloseSelectedPaneCommand is RelayCommand command)
            {
                command.RaiseCanExecuteChanged();
            }
            
#if DEBUG
            var oldItem = e.OldValue as FlexiPaneItem;
            var newItem = e.NewValue as FlexiPaneItem;
            System.Diagnostics.Debug.WriteLine($"[FlexiPanel] SelectedItem changed: {oldItem?.PaneId ?? "null"} → {newItem?.PaneId ?? "null"}");
#endif
        }
    }
    
    private void OnSplitModeChangedInternal(bool isActive)
    {
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[FlexiPanel] Internal split mode changed: {isActive}");
#endif
        
        // Raise event (cancellable)
        var args = new SplitModeChangedEventArgs(isActive, !isActive)
        {
            RoutedEvent = SplitModeChangedEvent
        };
        RaiseEvent(args);
        
        if (args.Cancel)
        {
            // Cancel change - restore previous value (직접 속성 설정)
            IsSplitModeActive = !isActive;
            return;
        }
        
        
        // When split mode is activated, ensure we have a splittable pane
        if (isActive)
        {
            // Check if we already have any FlexiPaneItem (single or in container)
            if (RootContent is FlexiPaneItem existingPane)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FlexiPanel] Split mode activated - existing single FlexiPaneItem found, enabling split capability");
#endif
                // Just enable splitting on existing pane
                existingPane.CanSplit = true;
                // Split mode state is inherited through WPF binding
                
                // Select this pane if none is selected
                if (SelectedItem == null)
                {
                    existingPane.IsSelected = true;
                }
            }
            else if (RootContent is FlexiPaneContainer)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FlexiPanel] Split mode activated - existing FlexiPaneContainer found, enabling split capability on all panes");
#endif
                // Enable splitting on all panes within the container
                EnableSplitModeRecursively(RootContent, true);
            }
            else
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FlexiPanel] Split mode activated - no suitable pane structure found, creating new one");
#endif
                // Create new splittable pane only if we don't have one
                CreateSplittablePaneWithEvent();
            }
        }
        else
        {
            // When split mode is deactivated, preserve existing structure but disable splitting
            if (RootContent is FlexiPaneItem existingPane)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FlexiPanel] Split mode deactivated - disabling split capability on single pane");
#endif
                existingPane.CanSplit = false;
                // Split mode state is inherited through WPF binding
            }
            else if (RootContent is FlexiPaneContainer)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FlexiPanel] Split mode deactivated - preserving FlexiPaneContainer structure, disabling split capability on all panes");
#endif
                // Preserve the container structure but disable splitting on all panes
                EnableSplitModeRecursively(RootContent, false);
            }
            else
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FlexiPanel] Split mode deactivated - no pane structure exists, showing info content");
#endif
                // Show info content if no pane structure exists
                RootContent = CreateSimpleInfoContent();
            }
        }
        
        // WPF binding handles property inheritance automatically
    }
    

    #endregion

    #region Property Changed Handlers

    private static void OnRootContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FlexiPanel panel && e.NewValue is UIElement newContent)
        {
            // Apply current state to new content and child elements
            // WPF binding handles property inheritance automatically
        }
    }

    #endregion

    #region Routed Events

    /// <summary>
    /// Content request event
    /// </summary>
    public static readonly RoutedEvent ContentRequestedEvent =
        EventManager.RegisterRoutedEvent("ContentRequested", RoutingStrategy.Bubble,
            typeof(ContentRequestedEventHandler), typeof(FlexiPanel));

    public event ContentRequestedEventHandler ContentRequested
    {
        add { AddHandler(ContentRequestedEvent, value); }
        remove { RemoveHandler(ContentRequestedEvent, value); }
    }


    /// <summary>
    /// Panel closing event
    /// </summary>
    public static readonly RoutedEvent PaneClosingEvent =
        EventManager.RegisterRoutedEvent("PaneClosing", RoutingStrategy.Bubble,
            typeof(PaneClosingEventHandler), typeof(FlexiPanel));

    public event PaneClosingEventHandler PaneClosing
    {
        add { AddHandler(PaneClosingEvent, value); }
        remove { RemoveHandler(PaneClosingEvent, value); }
    }

    /// <summary>
    /// Last panel closing event
    /// </summary>
    public static readonly RoutedEvent LastPaneClosingEvent =
        EventManager.RegisterRoutedEvent("LastPaneClosing", RoutingStrategy.Bubble,
            typeof(LastPaneClosingEventHandler), typeof(FlexiPanel));

    public event LastPaneClosingEventHandler LastPaneClosing
    {
        add { AddHandler(LastPaneClosingEvent, value); }
        remove { RemoveHandler(LastPaneClosingEvent, value); }
    }

    /// <summary>
    /// Split mode changed event
    /// </summary>
    public static readonly RoutedEvent SplitModeChangedEvent =
        EventManager.RegisterRoutedEvent("SplitModeChanged", RoutingStrategy.Bubble,
            typeof(SplitModeChangedEventHandler), typeof(FlexiPanel));

    public event SplitModeChangedEventHandler SplitModeChanged
    {
        add { AddHandler(SplitModeChangedEvent, value); }
        remove { RemoveHandler(SplitModeChangedEvent, value); }
    }

    /// <summary>
    /// New panel created event
    /// </summary>
    public static readonly RoutedEvent NewPaneCreatedEvent =
        EventManager.RegisterRoutedEvent("NewPaneCreated", RoutingStrategy.Bubble,
            typeof(NewPaneCreatedEventHandler), typeof(FlexiPanel));

    public event NewPaneCreatedEventHandler NewPaneCreated
    {
        add { AddHandler(NewPaneCreatedEvent, value); }
        remove { RemoveHandler(NewPaneCreatedEvent, value); }
    }

    #endregion

    #region Event Handlers

    private void OnContentRequested(object? sender, ContentRequestedEventArgs e)
    {
        if (e.RequestType == ContentRequestType.SplitPane)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[FlexiPanel] RECEIVED SPLIT CONTENT REQUEST - IsVertical: {e.IsVerticalSplit}, Ratio: {e.SplitRatio:F2}");
            System.Diagnostics.Debug.WriteLine($"[FlexiPanel] Content already set: {e.RequestedContent != null} (Type: {e.RequestedContent?.GetType().Name ?? "null"})");
#endif
            
            // Skip if event is already handled
            if (e.Handled)
                return;
            
            // Use Dispatcher to delay processing so other handlers can execute first
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
            {
                // Skip if event is cancelled or already handled
                if (e.Cancel || e.Handled)
                    return;
                    
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FlexiPanel] DELAYED PROCESSING - Content type after other handlers: {e.RequestedContent?.GetType().Name ?? "null"}");
#endif
                
                // Call FlexiPaneManager's split processing method
                if (e.SourcePane != null && e.IsVerticalSplit.HasValue && e.SplitRatio.HasValue && !e.Cancel)
                {
                    // Auto-generate content if none provided
                    UIElement? contentToUse = e.RequestedContent as System.Windows.UIElement;
                    if (contentToUse == null)
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine($"[FlexiPanel] Auto-generating content for new pane");
#endif
                        contentToUse = CreateAutoGeneratedPaneContent();
                        e.RequestedContent = contentToUse;
                    }
                    
                    var result = Managers.FlexiPaneManager.SplitPane(
                        e.SourcePane, 
                        e.IsVerticalSplit.Value, 
                        e.SplitRatio.Value, 
                        contentToUse);
                    
                    if (result == null)
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine($"[FlexiPanel] SPLIT FAILED - Setting Cancel = true");
#endif
                        e.Cancel = true;
                    }
                    else
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine($"[FlexiPanel] SPLIT SUCCESS - Container created");
#endif
                        e.Handled = true;
                    }
                }
            }));
        }
        else if (e.RequestType == ContentRequestType.InitialPane)
        {
            // Handle initial content requests - do nothing by default, let user handlers process
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[FlexiPanel] Initial content request: {e.Purpose}");
#endif
        }
    }

    private void OnPaneClosing(object? sender, PaneClosingEventArgs e)
    {
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[FlexiPanel] OnPaneClosing - Processing close request for pane");
#endif

        // Actual close processing through FlexiPaneManager
        if (e.Pane != null)
        {
            // First check total panel count
            var totalPanes = CountTotalPanes();
            if (totalPanes <= 1)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FlexiPanel] Last panel closing. Current count: {totalPanes}");
#endif
                // Raise last panel closing event
                var lastPaneArgs = new LastPaneClosingEventArgs(e.Pane, e.Reason)
                {
                    RoutedEvent = LastPaneClosingEvent
                };
                RaiseEvent(lastPaneArgs);
                
                if (lastPaneArgs.Cancel)
                {
                    // External cancellation request
                    e.Cancel = true;
                    return;
                }
                
                // Allow closing last panel (remove content)
                RootContent = null!;
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FlexiPanel] Last panel closed - RootContent cleared");
#endif
                return;
            }

            // Process close using simplified manager
            var success = Managers.FlexiPaneManager.ClosePane(e.Pane);
            if (!success)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FlexiPanel] ClosePane failed, cancelling close operation");
#endif
                e.Cancel = true;
            }
            else
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FlexiPanel] ClosePane succeeded. Remaining panes: {CountTotalPanes()}");
#endif
            }
        }
    }

    private void OnPanelPreviewKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.W && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            // Ctrl+W - Close the currently selected pane
            if (CloseSelectedPaneCommand?.CanExecute(null) == true)
            {
                CloseSelectedPaneCommand.Execute(null);
                e.Handled = true;
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FlexiPanel] Ctrl+W released (Preview) - Closing selected pane via FlexiPanel command");
#endif
            }
        }
    }

    #endregion

    #region Command Implementation

    private void ExecuteCloseSelectedPane(object? parameter)
    {
        if (SelectedItem != null && SelectedItem.CloseCommand?.CanExecute(null) == true)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[FlexiPanel] ExecuteCloseSelectedPane - Closing pane: {SelectedItem.PaneId}");
#endif
            SelectedItem.CloseCommand.Execute(null);
        }
    }

    private bool CanExecuteCloseSelectedPane(object? parameter)
    {
        return SelectedItem != null && 
               SelectedItem.CloseCommand?.CanExecute(null) == true;
    }

    #endregion

    #region Public Methods
    
    /// <summary>
    /// Split the currently selected pane vertically
    /// </summary>
    /// <param name="splitRatio">Split ratio (0.1 to 0.9), default 0.5</param>
    /// <param name="newContent">Content for the new pane</param>
    public void SplitSelectedVertically(double splitRatio = 0.5, UIElement? newContent = null)
    {
        if (SelectedItem != null && SelectedItem.CanSplit)
        {
            SelectedItem.Split(true, splitRatio, newContent);
        }
    }
    
    /// <summary>
    /// Split the currently selected pane horizontally
    /// </summary>
    /// <param name="splitRatio">Split ratio (0.1 to 0.9), default 0.5</param>
    /// <param name="newContent">Content for the new pane</param>
    public void SplitSelectedHorizontally(double splitRatio = 0.5, UIElement? newContent = null)
    {
        if (SelectedItem != null && SelectedItem.CanSplit)
        {
            SelectedItem.Split(false, splitRatio, newContent);
        }
    }
    
    /// <summary>
    /// Set the selected item (internal use)
    /// </summary>
    internal void SetSelectedItem(FlexiPaneItem item)
    {
        // Deselect previous item
        if (SelectedItem != null && SelectedItem != item)
        {
            SelectedItem.IsSelected = false;
        }
        
        SelectedItem = item;
    }

    /// <summary>
    /// Close the currently selected pane
    /// </summary>
    public void CloseSelectedPane()
    {
        if (CloseSelectedPaneCommand?.CanExecute(null) == true)
        {
            CloseSelectedPaneCommand.Execute(null);
        }
    }
    
    /// <summary>
    /// Find the first selectable pane
    /// </summary>
    public FlexiPaneItem? FindFirstSelectablePane()
    {
        return FindFirstSelectablePaneRecursive(RootContent);
    }
    
    private FlexiPaneItem? FindFirstSelectablePaneRecursive(UIElement? element)
    {
        if (element == null) return null;
        
        if (element is FlexiPaneItem item)
            return item;
            
        if (element is FlexiPaneContainer container)
        {
            var result = FindFirstSelectablePaneRecursive(container.FirstChild);
            if (result != null) return result;
            return FindFirstSelectablePaneRecursive(container.SecondChild);
        }
        
        return null;
    }
    
    #endregion

    #region Content Management Methods
    
    /// <summary>
    /// Create a splittable pane and request initial content
    /// </summary>
    private void CreateSplittablePaneWithEvent()
    {
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[FlexiPanel] CreateSplittablePaneWithEvent - Requesting initial content");
#endif
        
        // Request initial content from the application
        var contentEventArgs = new Events.ContentRequestedEventArgs("InitialPane")
        {
            RoutedEvent = ContentRequestedEvent
        };
        
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[FlexiPanel] CreateSplittablePaneWithEvent - Raising ContentRequested for initial content");
#endif
        
        // Raise the event to get content from the application  
        RaiseEvent(contentEventArgs);
        
        // Handle the requested content with automatic wrapping if needed
        FlexiPaneItem initialPane;
        if (contentEventArgs.RequestedContent is UIElement requestedContent)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[FlexiPanel] CreateSplittablePaneWithEvent - Got content from event: {requestedContent.GetType().Name}");
#endif
            
            // FlexiPaneItem이면 그대로 사용, 아니면 래핑
            if (requestedContent is FlexiPaneItem existingPane)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FlexiPanel] CreateSplittablePaneWithEvent - Content is already FlexiPaneItem, using directly");
#endif
                initialPane = existingPane;
                initialPane.CanSplit = true; // 분할 가능하도록 설정
            }
            else
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FlexiPanel] CreateSplittablePaneWithEvent - Wrapping content in FlexiPaneItem for splitting capability");
#endif
                initialPane = new FlexiPaneItem
                {
                    Title = "Main Panel",
                    CanSplit = IsSplitModeActive,
                    Content = requestedContent
                };
                // Split mode inherited through binding
            }
        }
        else
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[FlexiPanel] CreateSplittablePaneWithEvent - No content provided, using default");
#endif
            initialPane = new FlexiPaneItem
            {
                Title = "Main Panel",
                CanSplit = IsSplitModeActive,
                Content = CreateSamplePaneContent("Default Content", Colors.CornflowerBlue)
            };
            // Split mode inherited through binding
        }
        
        // Split mode inherited through binding automatically
        
        // Set as root content
        RootContent = initialPane;
        initialPane.IsSelected = true;
    }
    
    
    /// <summary>
    /// Check if the current content is a default content that can be replaced
    /// </summary>
    private bool IsDefaultContent(UIElement? content)
    {
        if (content == null) return true;
        
        // Check if it's a Border with the default text content
        if (content is Border border && 
            border.Background is SolidColorBrush brush &&
            brush.Color == Color.FromRgb(245, 245, 245) &&
            border.Child is TextBlock textBlock &&
            textBlock.Text.Contains("FlexiPane - Dynamic Panel Splitter"))
        {
            return true;
        }
        
        // Check if it's a default FlexiPaneItem with sample content
        if (content is FlexiPaneItem paneItem &&
            paneItem.Title == "Main Panel" &&
            paneItem.Content is Border paneContent &&
            paneContent.Background is SolidColorBrush paneBackground &&
            paneBackground.Color == Colors.DodgerBlue)
        {
            return true;
        }
        
        return false;
    }
    
    
    #endregion

    #region Split Mode Management
    
    /// <summary>
    /// Recursively enable or disable split mode on all FlexiPaneItem elements
    /// </summary>
    private void EnableSplitModeRecursively(UIElement? element, bool enableSplitMode)
    {
        if (element == null) return;
        
        switch (element)
        {
            case FlexiPaneItem paneItem:
                paneItem.CanSplit = enableSplitMode;
                // If enabling and no item is selected, select this one
                if (enableSplitMode && SelectedItem == null)
                {
                    paneItem.IsSelected = true;
                }
                break;
                
            case FlexiPaneContainer container:
                EnableSplitModeRecursively(container.FirstChild, enableSplitMode);
                EnableSplitModeRecursively(container.SecondChild, enableSplitMode);
                break;
        }
    }
    
    #endregion

    #region Helper Methods

    /// <summary>
    /// Find the nearest FlexiPanel ancestor
    /// </summary>
    public static FlexiPanel? FindAncestorPanel(DependencyObject element)
    {
        var current = element;

        while (current != null)
        {
            if (current is FlexiPanel panel)
                return panel;

            current = System.Windows.Media.VisualTreeHelper.GetParent(current) ??
                     LogicalTreeHelper.GetParent(current);
        }

        return null;
    }

    /// <summary>
    /// Count total number of panels
    /// </summary>
    public int CountTotalPanes()
    {
        return CountPanesRecursively(RootContent);
    }

    private int CountPanesRecursively(UIElement? element)
    {
        if (element == null)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[FlexiPanel] CountPanes - null element, returning 0");
#endif
            return 0;
        }

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[FlexiPanel] CountPanes - Examining element: {element.GetType().Name}");
#endif

        switch (element)
        {
            case FlexiPaneItem:
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FlexiPanel] CountPanes - Found FlexiPaneItem, returning 1");
#endif
                return 1;

            case FlexiPaneContainer container:
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FlexiPanel] CountPanes - Found FlexiPaneContainer, checking children");
                System.Diagnostics.Debug.WriteLine($"[FlexiPanel] CountPanes - FirstChild: {container.FirstChild?.GetType().Name ?? "null"}");
                System.Diagnostics.Debug.WriteLine($"[FlexiPanel] CountPanes - SecondChild: {container.SecondChild?.GetType().Name ?? "null"}");
#endif
                var firstCount = CountPanesRecursively(container.FirstChild);
                var secondCount = CountPanesRecursively(container.SecondChild);
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FlexiPanel] CountPanes - FirstChild count: {firstCount}, SecondChild count: {secondCount}, Total: {firstCount + secondCount}");
#endif
                return firstCount + secondCount;

            default:
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FlexiPanel] CountPanes - Unknown element type: {element.GetType().Name}, returning 0");
#endif
                return 0;
        }
    }

    /// <summary>
    /// Replace RootContent with new content and force complete UI update
    /// </summary>
    internal void UpdateRootContent(UIElement? newContent)
    {
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[FlexiPanel] UpdateRootContent - Old: {RootContent?.GetType().Name ?? "null"}, New: {newContent?.GetType().Name ?? "null"}");
#endif
        RootContent = newContent!;
        
        // Force immediate and complete UI tree update
        InvalidateVisual();
        InvalidateMeasure();
        InvalidateArrange();
        UpdateLayout();
        
        // Schedule additional template applications after the UI has been updated
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, new System.Action(() =>
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[FlexiPanel] Deferred RootContent template application");
#endif
            if (newContent != null)
            {
                // Apply template if it's a templated control
                if (newContent is Control control)
                {
                    control.ApplyTemplate();
                    control.UpdateLayout();
                }
                
                // Also apply to all children recursively
                ApplyTemplatesRecursively(newContent);
            }
        }));
    }
    
    /// <summary>
    /// Recursively apply templates to all controls in the tree
    /// </summary>
    private void ApplyTemplatesRecursively(UIElement element)
    {
        if (element == null) return;
        
        switch (element)
        {
            case FlexiPaneContainer container:
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FlexiPanel] ApplyTemplatesRecursively - FlexiPaneContainer");
#endif
                container.ApplyTemplate();
                container.UpdateLayout();
                if (container.FirstChild != null)
                    ApplyTemplatesRecursively(container.FirstChild);
                if (container.SecondChild != null)
                    ApplyTemplatesRecursively(container.SecondChild);
                break;
                
            case FlexiPaneItem paneItem:
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FlexiPanel] ApplyTemplatesRecursively - FlexiPaneItem");
#endif
                paneItem.ApplyTemplate();
                paneItem.UpdateLayout();
                break;
                
            case Control control:
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FlexiPanel] ApplyTemplatesRecursively - Control: {control.GetType().Name}");
#endif
                control.ApplyTemplate();
                control.UpdateLayout();
                break;
        }
    }

    // Removed complex property propagation - WPF binding handles this automatically

    #endregion
}