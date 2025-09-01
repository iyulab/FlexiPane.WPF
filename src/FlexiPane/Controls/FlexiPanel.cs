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
    private bool _isUpdatingSelection = false; // Prevent recursion in selection updates
    
    /// <summary>
    /// Gets whether the panel is currently updating selection (for internal use by FlexiPaneItem)
    /// </summary>
    internal bool IsUpdatingSelection => _isUpdatingSelection;
    
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
            // Try to get initial content from ContentRequested event now that handlers are connected
            if (IsDefaultContent(RootContent))
            {
                var contentEventArgs = new Events.ContentRequestedEventArgs("InitialContent")
                {
                    RoutedEvent = ContentRequestedEvent
                };
                
                // Raise the event to get content from the application  
                RaiseEvent(contentEventArgs);
                
                // Use requested content if provided
                if (contentEventArgs.RequestedContent is UIElement requestedContent)
                {
                    // FlexiPaneItem이 아니면 자동으로 래핑
                    if (requestedContent is FlexiPaneItem)
                    {
                        RootContent = requestedContent;
                    }
                    else
                    {
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
            }
            
            // Apply current split mode state to existing content if any
            if (RootContent is FlexiPaneItem paneItem)
            {
                // Split mode is automatically inherited through WPF property inheritance
                
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

    /// <summary>
    /// Preferred split direction - true for vertical, false for horizontal
    /// </summary>
    public bool PreferredSplitDirection
    {
        get { return (bool)GetValue(PreferredSplitDirectionProperty); }
        set { SetValue(PreferredSplitDirectionProperty, value); }
    }

    public static readonly DependencyProperty PreferredSplitDirectionProperty =
        DependencyProperty.Register(nameof(PreferredSplitDirection), typeof(bool), typeof(FlexiPanel),
            new PropertyMetadata(false)); // Default to horizontal split

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
        }
    }
    
    private void OnSplitModeChangedInternal(bool isActive)
    {
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
                // Enable splitting on all panes within the container
                EnableSplitModeRecursively(RootContent, true);
            }
            else
            {
                // Create new splittable pane only if we don't have one
                CreateSplittablePaneWithEvent();
            }
        }
        else
        {
            // When split mode is deactivated, preserve existing structure but disable splitting
            if (RootContent is FlexiPaneItem existingPane)
            {
                existingPane.CanSplit = false;
                // Split mode state is inherited through WPF binding
            }
            else if (RootContent is FlexiPaneContainer)
            {
                // Preserve the container structure but disable splitting on all panes
                EnableSplitModeRecursively(RootContent, false);
            }
            else
            {
                // Show info content if no pane structure exists
                RootContent = CreateSimpleInfoContent();
            }
        }
        
        // WPF binding handles property inheritance automatically
        
        // Validate selection consistency after split mode change
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
        {
            ValidateAndRepairSelection();
        }));
    }
    

    #endregion

    #region Property Changed Handlers

    private static void OnRootContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FlexiPanel panel && e.NewValue is UIElement newContent)
        {
            // Apply current state to new content and child elements
            // WPF binding handles property inheritance automatically
            
            // Validate and repair selection after root content change
            panel.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
            {
                panel.ValidateAndRepairSelection();
            }));
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
            // Skip if event is already handled
            if (e.Handled)
                return;
            
            // Use Dispatcher to delay processing so other handlers can execute first
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
            {
                // Skip if event is cancelled or already handled
                if (e.Cancel || e.Handled)
                    return;
                
                // Call FlexiPaneManager's split processing method
                if (e.SourcePane != null && e.IsVerticalSplit.HasValue && e.SplitRatio.HasValue && !e.Cancel)
                {
                    // Auto-generate content if none provided
                    UIElement? contentToUse = e.RequestedContent as System.Windows.UIElement;
                    if (contentToUse == null)
                    {
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
                        e.Cancel = true;
                    }
                    else
                    {
                        e.Handled = true;
                        
                        // Validate and repair selection after split operation
                        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
                        {
                            ValidateAndRepairSelection();
                        }));
                    }
                }
            }));
        }
        else if (e.RequestType == ContentRequestType.InitialPane)
        {
            // Handle initial content requests - do nothing by default, let user handlers process
        }
    }

    private void OnPaneClosing(object? sender, PaneClosingEventArgs e)
    {
        // Actual close processing through FlexiPaneManager
        if (e.Pane != null)
        {
            // First check total panel count
            var totalPanes = CountTotalPanes();
            if (totalPanes <= 1)
            {
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
                return;
            }

            // Process close using simplified manager
            var success = Managers.FlexiPaneManager.ClosePane(e.Pane);
            if (!success)
            {
                e.Cancel = true;
            }
            else
            {
                // Validate and repair selection after close operation
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
                {
                    ValidateAndRepairSelection();
                }));
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
            }
        }
        else if (e.Key == Key.Space && Keyboard.Modifiers == ModifierKeys.None && IsSplitModeActive)
        {
            // Space - Toggle split direction when in split mode
            ToggleSplitDirection();
            e.Handled = true;
        }
    }

    #endregion

    #region Command Implementation

    private void ExecuteCloseSelectedPane(object? parameter)
    {
        if (SelectedItem != null && SelectedItem.CloseCommand?.CanExecute(null) == true)
        {
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
    /// Split the currently selected pane
    /// </summary>
    /// <param name="isVerticalSplit">True for vertical split, false for horizontal split</param>
    /// <param name="splitRatio">Split ratio (0.1 to 0.9), default 0.5</param>
    /// <param name="newContent">Content for the new pane</param>
    public void Split(bool isVerticalSplit, double splitRatio = 0.5, UIElement? newContent = null)
    {
        // Ensure we have a splittable pane and prepare for splitting
        PrepareForSplitting();
        
        if (SelectedItem != null && SelectedItem.CanSplit)
        {
            SelectedItem.SplitInternal(isVerticalSplit, splitRatio, newContent);
        }
    }
    
    /// <summary>
    /// Split the currently selected pane vertically
    /// </summary>
    /// <param name="splitRatio">Split ratio (0.1 to 0.9), default 0.5</param>
    /// <param name="newContent">Content for the new pane</param>
    public void SplitSelectedVertically(double splitRatio = 0.5, UIElement? newContent = null)
    {
        Split(true, splitRatio, newContent);
    }
    
    /// <summary>
    /// Split the currently selected pane horizontally
    /// </summary>
    /// <param name="splitRatio">Split ratio (0.1 to 0.9), default 0.5</param>
    /// <param name="newContent">Content for the new pane</param>
    public void SplitSelectedHorizontally(double splitRatio = 0.5, UIElement? newContent = null)
    {
        Split(false, splitRatio, newContent);
    }
    
    /// <summary>
    /// Set the selected item (internal use) with comprehensive selection management
    /// </summary>
    internal void SetSelectedItem(FlexiPaneItem item)
    {
        // Prevent recursion during selection updates
        if (_isUpdatingSelection)
        {
            return;
        }
        
        // Prevent recursion and verify the item is actually in our tree
        if (item != null && !IsItemInTree(item))
        {
            return;
        }
        
        try
        {
            _isUpdatingSelection = true;
            
            // Ensure only one item is selected across the entire tree
            EnsureSingleSelectionInternal(item);
            
            SelectedItem = item;
        }
        finally
        {
            _isUpdatingSelection = false;
        }
    }
    
    /// <summary>
    /// Verifies that a FlexiPaneItem is actually in the current tree
    /// </summary>
    private bool IsItemInTree(FlexiPaneItem item)
    {
        var allItems = GetAllPaneItems();
        return allItems.Contains(item);
    }
    
    /// <summary>
    /// Ensures only one FlexiPaneItem is selected across the entire child tree
    /// </summary>
    private void EnsureSingleSelection(FlexiPaneItem? targetItem)
    {
        if (_isUpdatingSelection) return;
        
        try
        {
            _isUpdatingSelection = true;
            EnsureSingleSelectionInternal(targetItem);
        }
        finally
        {
            _isUpdatingSelection = false;
        }
    }
    
    /// <summary>
    /// Internal method to ensure single selection without recursion protection
    /// </summary>
    private void EnsureSingleSelectionInternal(FlexiPaneItem? targetItem)
    {
        // Clear all selections first
        ClearAllSelections(RootContent);
        
        // Set the target item as selected
        if (targetItem != null)
        {
            targetItem.SetCurrentValue(FlexiPaneItem.IsSelectedProperty, true);
        }
    }
    
    /// <summary>
    /// Recursively clears all selections in the child tree
    /// </summary>
    private void ClearAllSelections(UIElement? element)
    {
        if (element == null) return;
        
        try
        {
            switch (element)
            {
                case FlexiPaneItem paneItem:
                    // Directly set the backing field to prevent recursive events
                    if (paneItem.IsSelected)
                    {
                        paneItem.SetCurrentValue(FlexiPaneItem.IsSelectedProperty, false);
                    }
                    break;
                    
                case FlexiPaneContainer container:
                    ClearAllSelections(container.FirstChild);
                    ClearAllSelections(container.SecondChild);
                    break;
            }
        }
        catch (Exception)
        {
            // Silently handle exceptions during selection clearing
        }
    }
    
    /// <summary>
    /// Gets all FlexiPaneItem instances in the child tree
    /// </summary>
    public List<FlexiPaneItem> GetAllPaneItems()
    {
        var items = new List<FlexiPaneItem>();
        CollectPaneItems(RootContent, items);
        return items;
    }
    
    /// <summary>
    /// Recursively collects all FlexiPaneItem instances
    /// </summary>
    private void CollectPaneItems(UIElement? element, List<FlexiPaneItem> items)
    {
        if (element == null) return;
        
        switch (element)
        {
            case FlexiPaneItem paneItem:
                items.Add(paneItem);
                break;
                
            case FlexiPaneContainer container:
                CollectPaneItems(container.FirstChild, items);
                CollectPaneItems(container.SecondChild, items);
                break;
        }
    }
    
    /// <summary>
    /// Validates and repairs selection consistency across the entire tree
    /// </summary>
    public void ValidateAndRepairSelection()
    {
        // Prevent recursion during validation
        if (_isUpdatingSelection)
        {
            return;
        }
        
        try
        {
            _isUpdatingSelection = true;
            
            var allItems = GetAllPaneItems();
            var selectedItems = allItems.Where(item => item.IsSelected).ToList();
            
            if (selectedItems.Count > 1)
            {
                // Multiple selections found - keep only the most recently selected one (SelectedItem property)
                var itemToKeep = SelectedItem != null && selectedItems.Contains(SelectedItem) 
                    ? SelectedItem 
                    : selectedItems.FirstOrDefault();
                    
                EnsureSingleSelectionInternal(itemToKeep);
                SelectedItem = itemToKeep;
            }
            else if (selectedItems.Count == 0 && allItems.Count > 0)
            {
                // No selection but items exist - select the first available item
                var firstItem = allItems.FirstOrDefault();
                if (firstItem != null)
                {
                    EnsureSingleSelectionInternal(firstItem);
                    SelectedItem = firstItem;
                }
            }
            else if (selectedItems.Count == 1)
            {
                // Exactly one selection - ensure SelectedItem property is in sync
                var selectedItem = selectedItems[0];
                if (SelectedItem != selectedItem)
                {
                    SelectedItem = selectedItem;
                }
            }
        }
        catch (Exception)
        {
            // Silently handle validation errors
        }
        finally
        {
            _isUpdatingSelection = false;
        }
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
    /// Toggle split direction between vertical and horizontal
    /// </summary>
    public void ToggleSplitDirection()
    {
        PreferredSplitDirection = !PreferredSplitDirection;
        
        // Update all FlexiPaneItems with the new direction
        UpdateAllPaneItemDirections(PreferredSplitDirection);
    }
    
    /// <summary>
    /// Update all FlexiPaneItem directions when Space key is pressed
    /// </summary>
    private void UpdateAllPaneItemDirections(bool newDirection)
    {
        UpdatePaneItemDirectionsRecursive(RootContent, newDirection);
    }
    
    private void UpdatePaneItemDirectionsRecursive(UIElement? element, bool newDirection)
    {
        if (element == null) return;
        
        if (element is FlexiPaneItem paneItem)
        {
            paneItem.UpdateCurrentSplitDirection(newDirection);
        }
        else if (element is FlexiPaneContainer container)
        {
            UpdatePaneItemDirectionsRecursive(container.FirstChild, newDirection);
            UpdatePaneItemDirectionsRecursive(container.SecondChild, newDirection);
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

    #region Split Preparation Methods
    
    /// <summary>
    /// Prepare the panel for splitting operations
    /// Ensures we have a valid FlexiPaneItem that can be split
    /// </summary>
    private void PrepareForSplitting()
    {
        // If we don't have any content, create initial splittable content
        if (RootContent == null)
        {
            CreateSplittablePaneWithEvent();
            return;
        }

        // If RootContent is not a FlexiPaneItem or FlexiPaneContainer, wrap it
        if (RootContent is not FlexiPaneItem && RootContent is not FlexiPaneContainer)
        {
            var wrappedItem = new FlexiPaneItem
            {
                Title = "Main Panel",
                CanSplit = true,
                Content = RootContent
            };
            RootContent = wrappedItem;
            wrappedItem.IsSelected = true;
            return;
        }

        // Ensure we have a selected item
        if (SelectedItem == null)
        {
            var firstPane = FindFirstSelectablePane();
            if (firstPane != null)
            {
                firstPane.IsSelected = true;
            }
            else
            {
                CreateSplittablePaneWithEvent();
                return;
            }
        }

        // Ensure the selected item can split
        if (SelectedItem != null && !SelectedItem.CanSplit)
        {
            SelectedItem.CanSplit = true;
        }
    }
    
    #endregion

    #region Content Management Methods
    
    /// <summary>
    /// Create a splittable pane and request initial content
    /// </summary>
    private void CreateSplittablePaneWithEvent()
    {
        // Request initial content from the application
        var contentEventArgs = new Events.ContentRequestedEventArgs("InitialPane")
        {
            RoutedEvent = ContentRequestedEvent
        };
        
        // Raise the event to get content from the application  
        RaiseEvent(contentEventArgs);
        
        // Handle the requested content with automatic wrapping if needed
        FlexiPaneItem initialPane;
        if (contentEventArgs.RequestedContent is UIElement requestedContent)
        {
            // FlexiPaneItem이면 그대로 사용, 아니면 래핑
            if (requestedContent is FlexiPaneItem existingPane)
            {
                initialPane = existingPane;
                initialPane.CanSplit = true; // 분할 가능하도록 설정
            }
            else
            {
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
            return 0;
        }

        switch (element)
        {
            case FlexiPaneItem:
                return 1;

            case FlexiPaneContainer container:
                var firstCount = CountPanesRecursively(container.FirstChild);
                var secondCount = CountPanesRecursively(container.SecondChild);
                return firstCount + secondCount;

            default:
                return 0;
        }
    }

    /// <summary>
    /// Replace RootContent with new content and force complete UI update
    /// </summary>
    internal void UpdateRootContent(UIElement? newContent)
    {
        RootContent = newContent!;
        
        // Force immediate and complete UI tree update
        InvalidateVisual();
        InvalidateMeasure();
        InvalidateArrange();
        UpdateLayout();
        
        // Schedule additional template applications after the UI has been updated
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, new System.Action(() =>
        {
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
                container.ApplyTemplate();
                container.UpdateLayout();
                if (container.FirstChild != null)
                    ApplyTemplatesRecursively(container.FirstChild);
                if (container.SecondChild != null)
                    ApplyTemplatesRecursively(container.SecondChild);
                break;
                
            case FlexiPaneItem paneItem:
                paneItem.ApplyTemplate();
                paneItem.UpdateLayout();
                break;
                
            case Control control:
                control.ApplyTemplate();
                control.UpdateLayout();
                break;
        }
    }

    // Removed complex property propagation - WPF binding handles this automatically

    #endregion
}