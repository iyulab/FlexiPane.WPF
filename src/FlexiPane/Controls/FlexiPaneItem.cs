using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using FlexiPane.Commands;
using FlexiPane.Events;

namespace FlexiPane.Controls;

/// <summary>
/// A splittable panel that contains actual content
/// Provides split mode UI and is replaced with FlexiPaneContainer when split
/// </summary>
[TemplatePart(Name = "PART_ContainerGrid", Type = typeof(Grid))]
[TemplatePart(Name = "PART_ContentBorder", Type = typeof(Border))]
[TemplatePart(Name = "PART_SplitOverlay", Type = typeof(Grid))]
[TemplatePart(Name = "PART_CloseButton", Type = typeof(Button))]
public class FlexiPaneItem : ContentControl, IDisposable
{
    static FlexiPaneItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(FlexiPaneItem), 
            new FrameworkPropertyMetadata(typeof(FlexiPaneItem)));
        
        // Make FlexiPaneItem focusable by default
        FocusableProperty.OverrideMetadata(typeof(FlexiPaneItem),
            new FrameworkPropertyMetadata(true));
    }

    private bool _isDisposed = false;
    private Grid? _containerGrid;
    private Border? _contentBorder;
    private Grid? _splitOverlay;
    private Button? _closeButton;
    private Rectangle? _verticalGuideLine;
    private Rectangle? _horizontalGuideLine;
    private Border? _defaultGuidePanel;
    private bool _currentSplitDirection = true; // true = vertical, false = horizontal

    public FlexiPaneItem()
    {
        PaneId = Guid.NewGuid().ToString();
        
        // Initialize commands
        CloseCommand = new RelayCommand(ExecuteClose, CanExecuteClose);
        SplitVerticalCommand = new RelayCommand(_ => RequestSplit(true), _ => CanSplitNow());
        SplitHorizontalCommand = new RelayCommand(_ => RequestSplit(false), _ => CanSplitNow());

        // Set default values - will be updated by FlexiPanel when added to tree
        CanSplit = false;
        
        // Subscribe to events
        this.Loaded += OnLoaded;
        this.Unloaded += OnUnloaded;
    }

    #region Dependency Properties
    
    /// <summary>
    /// Indicators whether this pane is currently selected/focused
    /// </summary>
    public bool IsSelected
    {
        get { return (bool)GetValue(IsSelectedProperty); }
        set { SetValue(IsSelectedProperty, value); }
    }

    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(FlexiPaneItem),
            new PropertyMetadata(false, OnIsSelectedChanged));

    private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FlexiPaneItem item)
        {
            bool newValue = (bool)e.NewValue;
            bool oldValue = (bool)e.OldValue;
            
            // Only process selection (true), not deselection to prevent recursion
            if (newValue && !oldValue)
            {
                // Notify FlexiPanel about selection change
                var panel = FlexiPanel.FindAncestorPanel(item);
                if (panel != null)
                {
                    // Prevent recursion if panel is already updating selection
                    if (panel.IsUpdatingSelection)
                    {
                        return;
                    }
                    
                    panel.SetSelectedItem(item);
                }
            }
        }
    }

    /// <summary>
    /// Panel unique identifier
    /// </summary>
    public string PaneId
    {
        get { return (string)GetValue(PaneIdProperty); }
        set { SetValue(PaneIdProperty, value); }
    }

    public static readonly DependencyProperty PaneIdProperty =
        DependencyProperty.Register(nameof(PaneId), typeof(string), typeof(FlexiPaneItem),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Panel title
    /// </summary>
    public string Title
    {
        get { return (string)GetValue(TitleProperty); }
        set { SetValue(TitleProperty, value); }
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(FlexiPaneItem),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Whether this panel can be split (local state)
    /// </summary>
    public bool CanSplit
    {
        get { return (bool)GetValue(CanSplitProperty); }
        set { SetValue(CanSplitProperty, value); }
    }

    public static readonly DependencyProperty CanSplitProperty =
        DependencyProperty.Register(nameof(CanSplit), typeof(bool), typeof(FlexiPaneItem),
            new PropertyMetadata(false));

    /// <summary>
    /// Custom content for split mode guide panel
    /// </summary>
    public object? SplitGuideContent
    {
        get { return GetValue(SplitGuideContentProperty); }
        set { SetValue(SplitGuideContentProperty, value); }
    }

    public static readonly DependencyProperty SplitGuideContentProperty =
        DependencyProperty.Register(nameof(SplitGuideContent), typeof(object), typeof(FlexiPaneItem),
            new PropertyMetadata(null, OnSplitGuideContentChanged));

    private static void OnSplitGuideContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FlexiPaneItem pane && !pane._isDisposed)
        {
            pane.UpdateGuidePanel();
        }
    }

    /// <summary>
    /// Custom content template for split mode guide panel
    /// </summary>
    public DataTemplate? SplitGuideContentTemplate
    {
        get { return (DataTemplate?)GetValue(SplitGuideContentTemplateProperty); }
        set { SetValue(SplitGuideContentTemplateProperty, value); }
    }

    public static readonly DependencyProperty SplitGuideContentTemplateProperty =
        DependencyProperty.Register(nameof(SplitGuideContentTemplate), typeof(DataTemplate), typeof(FlexiPaneItem),
            new PropertyMetadata(null));

    /// <summary>
    /// Whether to show default split guide panel
    /// </summary>
    public bool ShowDefaultGuidePanel
    {
        get { return (bool)GetValue(ShowDefaultGuidePanelProperty); }
        set { SetValue(ShowDefaultGuidePanelProperty, value); }
    }

    public static readonly DependencyProperty ShowDefaultGuidePanelProperty =
        DependencyProperty.Register(nameof(ShowDefaultGuidePanel), typeof(bool), typeof(FlexiPaneItem),
            new PropertyMetadata(true));


    #endregion

    #region Command Properties

    /// <summary>
    /// Close panel command
    /// </summary>
    public ICommand CloseCommand
    {
        get { return (ICommand)GetValue(CloseCommandProperty); }
        set { SetValue(CloseCommandProperty, value); }
    }

    public static readonly DependencyProperty CloseCommandProperty =
        DependencyProperty.Register(nameof(CloseCommand), typeof(ICommand), typeof(FlexiPaneItem),
            new PropertyMetadata(null));

    /// <summary>
    /// Vertical split command
    /// </summary>
    public ICommand SplitVerticalCommand
    {
        get { return (ICommand)GetValue(SplitVerticalCommandProperty); }
        set { SetValue(SplitVerticalCommandProperty, value); }
    }

    public static readonly DependencyProperty SplitVerticalCommandProperty =
        DependencyProperty.Register(nameof(SplitVerticalCommand), typeof(ICommand), typeof(FlexiPaneItem),
            new PropertyMetadata(null));

    /// <summary>
    /// Horizontal split command
    /// </summary>
    public ICommand SplitHorizontalCommand
    {
        get { return (ICommand)GetValue(SplitHorizontalCommandProperty); }
        set { SetValue(SplitHorizontalCommandProperty, value); }
    }

    public static readonly DependencyProperty SplitHorizontalCommandProperty =
        DependencyProperty.Register(nameof(SplitHorizontalCommand), typeof(ICommand), typeof(FlexiPaneItem),
            new PropertyMetadata(null));

    #endregion

    #region Events

    /// <summary>
    /// Panel closed event
    /// </summary>
    public event EventHandler<PaneClosedEventArgs>? Closed;

    #endregion

    #region Template Handling

    public override void OnApplyTemplate()
    {
        if (_isDisposed) return;

        base.OnApplyTemplate();

        // Remove existing event handlers
        RemoveEventHandlers();

        // Get template elements
        _containerGrid = GetTemplateChild("PART_ContainerGrid") as Grid;
        _contentBorder = GetTemplateChild("PART_ContentBorder") as Border;
        _splitOverlay = GetTemplateChild("PART_SplitOverlay") as Grid;
        _closeButton = GetTemplateChild("PART_CloseButton") as Button;
        _verticalGuideLine = GetTemplateChild("PART_VerticalGuideLine") as Rectangle;
        _horizontalGuideLine = GetTemplateChild("PART_HorizontalGuideLine") as Rectangle;
        _defaultGuidePanel = GetTemplateChild("PART_DefaultGuidePanel") as Border;

        // Connect event handlers
        ConnectEventHandlers();

        // Set keyboard focus
        this.Focusable = true;
        
        // Initialize split mode state when template is applied
        InitializeSplitModeState();
    }
    
    /// <summary>
    /// Initialize split mode state when template is first applied or split mode becomes active
    /// </summary>
    private void InitializeSplitModeState()
    {
        var flexiPanel = FlexiPanel.FindAncestorPanel(this);
        var isSplitModeActive = flexiPanel?.IsSplitModeActive ?? false;
        
        if (isSplitModeActive && _splitOverlay != null && _splitOverlay.Visibility == Visibility.Visible)
        {
            // Reset to default vertical split direction when split mode is activated
            _currentSplitDirection = true;
            UpdateSplitModeGuideText();
            
            // Set focus when split mode is activated
            this.Focus();
            Keyboard.Focus(this);
        }
    }

    private void ConnectEventHandlers()
    {
        if (_splitOverlay != null)
        {
            _splitOverlay.MouseLeftButtonUp += OnSplitOverlayClick;
            _splitOverlay.MouseMove += OnSplitOverlayMouseMove;
            _splitOverlay.MouseLeave += OnSplitOverlayMouseLeave;
            _splitOverlay.MouseEnter += OnSplitOverlayMouseEnter;
            _splitOverlay.IsVisibleChanged += OnSplitOverlayVisibilityChanged;
        }

        this.PreviewKeyUp += OnPreviewKeyUp;
        
        // Handle focus events for selection
        this.GotFocus += OnGotFocus;
        this.MouseDown += OnMouseDown;
    }

    private void RemoveEventHandlers()
    {
        if (_splitOverlay != null)
        {
            _splitOverlay.MouseLeftButtonUp -= OnSplitOverlayClick;
            _splitOverlay.MouseMove -= OnSplitOverlayMouseMove;
            _splitOverlay.MouseLeave -= OnSplitOverlayMouseLeave;
            _splitOverlay.MouseEnter -= OnSplitOverlayMouseEnter;
            _splitOverlay.IsVisibleChanged -= OnSplitOverlayVisibilityChanged;
        }

        this.PreviewKeyUp -= OnPreviewKeyUp;
        this.GotFocus -= OnGotFocus;
        this.MouseDown -= OnMouseDown;
    }

    #endregion

    #region Event Handlers

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Auto-connect events to FlexiPaneManager on load
        Managers.FlexiPaneManager.ConnectPaneEvents(this);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        // Disconnect events from FlexiPaneManager on unload
        Managers.FlexiPaneManager.DisconnectPaneEvents(this);
    }


    private void OnSplitOverlayClick(object sender, MouseButtonEventArgs e)
    {
        // Split overlay에서 이벤트가 발생했다는 것은 이미 분할 모드가 활성화된 것
        if (!CanSplit)
        {
            return;
        }

        // Determine split direction based on click position
        var position = e.GetPosition(_splitOverlay);
        var width = _splitOverlay!.ActualWidth;
        var height = _splitOverlay!.ActualHeight;

        // Edge area size (same as UpdateGuideLines)
        const double edgeThreshold = 24;

        // Check if click is in guide areas (edges)
        bool isLeftEdge = position.X <= edgeThreshold;
        bool isRightEdge = position.X >= width - edgeThreshold;
        bool isTopEdge = position.Y <= edgeThreshold;
        bool isBottomEdge = position.Y >= height - edgeThreshold;

        // Skip split if clicking on edge areas (direction already changed on hover)
        if (isLeftEdge || isRightEdge || isTopEdge || isBottomEdge)
        {
            return; // Don't perform split on edge areas
        }

        // Center area clicked - perform split using current direction
        bool isVerticalSplit = _currentSplitDirection;
        double splitRatio;

        if (isVerticalSplit)
        {
            // Vertical split - use X position for ratio
            splitRatio = position.X / width;
        }
        else
        {
            // Horizontal split - use Y position for ratio
            splitRatio = position.Y / height;
        }

        // Limit split ratio range
        splitRatio = Math.Max(0.1, Math.Min(0.9, splitRatio));

        RequestSplit(isVerticalSplit, splitRatio);
    }

    private void OnPreviewKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            var flexiPanel = FlexiPanel.FindAncestorPanel(this);
            if (flexiPanel != null && flexiPanel.IsSplitModeActive)
            {
                flexiPanel.IsSplitModeActive = false;
                e.Handled = true;
            }
        }
        else if (e.Key == Key.W && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            // Ctrl+W - Close the current pane (same as clicking close button)
            
            if (IsSelected && CloseCommand?.CanExecute(null) == true)
            {
                CloseCommand.Execute(null);
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Space && Keyboard.Modifiers == ModifierKeys.None)
        {
            var flexiPanel = FlexiPanel.FindAncestorPanel(this);
            if (flexiPanel != null && flexiPanel.IsSplitModeActive && IsSelected)
            {
                // Space - Toggle split direction when in split mode and this pane is selected
                flexiPanel.ToggleSplitDirection();
                e.Handled = true;
            }
        }
    }
    
    private void OnSplitOverlayMouseMove(object sender, MouseEventArgs e)
    {
        // Split overlay가 표시되어 있다면 이미 분할 모드가 활성화된 것
        // 불필요한 추가 검증 제거
        if (!CanSplit || _splitOverlay == null) 
        {
            return;
        }

        var position = e.GetPosition(_splitOverlay);
        var width = _splitOverlay.ActualWidth;
        var height = _splitOverlay.ActualHeight;

        // Edge area size
        const double edgeThreshold = 24;

        // Check if hovering over guide areas and auto-toggle direction
        bool isLeftEdge = position.X <= edgeThreshold;
        bool isRightEdge = position.X >= width - edgeThreshold;
        bool isTopEdge = position.Y <= edgeThreshold;
        bool isBottomEdge = position.Y >= height - edgeThreshold;

        bool directionChanged = false;

        if (isLeftEdge || isRightEdge)
        {
            // 좌우영역 오버 → 상하분할모드: HORIZONTAL split (가로선으로 나누어 상하로 분할)
            if (_currentSplitDirection != false)
            {
                _currentSplitDirection = false;
                directionChanged = true;
            }
        }
        else if (isTopEdge || isBottomEdge)
        {
            // 상하영역 오버 → 좌우분할모드: VERTICAL split (세로선으로 나누어 좌우로 분할)
            if (_currentSplitDirection != true)
            {
                _currentSplitDirection = true;
                directionChanged = true;
            }
        }
        else
        {
            // Not on any edge - MAINTAIN current split direction
            // The direction should only change when:
            // 1. Hovering over edge areas
            // 2. Space key is pressed (handled separately)
        }

        if (directionChanged)
        {
            UpdateSplitModeGuideText();
        }

        UpdateGuideLines(position);
    }

    private void OnSplitOverlayMouseLeave(object sender, MouseEventArgs e)
    {
        HideGuideLines();
    }
    
    private void OnSplitOverlayMouseEnter(object sender, MouseEventArgs e)
    {
    }
    
    private void OnGotFocus(object sender, RoutedEventArgs e)
    {
        if (IsSelected) return;

        // Mark this pane as selected when it receives focus
        IsSelected = true;

        // Ensure keyboard input is properly handled
        this.Focus();
        Keyboard.Focus(this);
    }
    
    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (IsSelected) return;

        // Select this pane on mouse click and set keyboard focus
        IsSelected = true;
        this.Focus();
        Keyboard.Focus(this);
    }

    private void OnSplitOverlayVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_splitOverlay != null && _splitOverlay.Visibility == Visibility.Visible)
        {
            // Split mode activated - initialize state with FlexiPanel's preferred direction
            var flexiPanel = FlexiPanel.FindAncestorPanel(this);
            _currentSplitDirection = flexiPanel?.PreferredSplitDirection ?? true; // Use panel's preferred direction or default to vertical
            UpdateSplitModeGuideText();
            
            // Set focus for ESC key handling
            this.Focus();
            Keyboard.Focus(this);
        }
        else
        {
            // Split mode deactivated - hide guide lines
            HideGuideLines();
        }
    }

    #endregion

    #region Commands Implementation

    private void ExecuteClose(object? parameter)
    {
        var flexiPanel = FlexiPanel.FindAncestorPanel(this);
        var isSplitModeActive = flexiPanel?.IsSplitModeActive ?? false;
        
        var args = new PaneClosingEventArgs(this, PaneCloseReason.UserRequest);
        OnClosing(args);

        if (!args.Cancel)
        {
            OnClosed(new PaneClosedEventArgs(this, PaneCloseReason.UserRequest));
        }
    }

    private bool CanExecuteClose(object? parameter)
    {
        // 패널이 존재하고 dispose되지 않았으면 언제든 닫기 가능
        // Split mode와 관계없이 닫기 허용
        bool canClose = !_isDisposed;
        return canClose;
    }

    private bool CanSplitNow()
    {
        // 단순화: CanSplit과 오버레이 가시성만 확인
        return CanSplit && _splitOverlay?.Visibility == Visibility.Visible && !_isDisposed;
    }

    private void RequestSplit(bool isVerticalSplit, double splitRatio = 0.5)
    {
        if (!CanSplitNow()) 
        {
            return;
        }

        // Use internal split method
        SplitInternal(isVerticalSplit, splitRatio);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Close panel
    /// </summary>
    public void Close()
    {
        if (CloseCommand?.CanExecute(null) == true)
        {
            CloseCommand.Execute(null);
        }
    }

    /// <summary>
    /// Split panel (internal use only - use FlexiPanel.Split() instead)
    /// </summary>
    internal void SplitInternal(bool isVerticalSplit, double splitRatio = 0.5, object? newContent = null)
    {
        var args = new ContentRequestedEventArgs(this, isVerticalSplit, splitRatio)
        {
            RequestedContent = newContent
        };
        
        // If content is already provided, skip ContentRequested event and handle split directly
        if (newContent != null)
        {
            // Directly process the split with provided content
            args.Handled = true;
            
            // Use FlexiPaneManager to handle the split directly
            var result = Managers.FlexiPaneManager.SplitPane(this, isVerticalSplit, splitRatio, newContent as UIElement);
            if (result != null)
            {
                // Find parent FlexiPanel and validate selection
                var panel = FlexiPanel.FindAncestorPanel(this);
                panel?.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
                {
                    panel.ValidateAndRepairSelection();
                }));
            }
            else
            {
                // Fall back to ContentRequested event if direct split fails
                OnSplitRequested(args);
            }
        }
        else
        {
            // No content provided, use ContentRequested event to get content
            OnSplitRequested(args);
        }
    }


    #endregion

    #region Protected Methods

    protected virtual void OnSplitRequested(ContentRequestedEventArgs e)
    {
        // Bubble up to FlexiPanel's Routed Event
        e.RoutedEvent = FlexiPanel.ContentRequestedEvent;
        RaiseEvent(e);
    }

    protected virtual void OnClosing(PaneClosingEventArgs e)
    {
        // Bubble up to FlexiPanel's Routed Event
        e.RoutedEvent = FlexiPanel.PaneClosingEvent;
        RaiseEvent(e);
    }

    protected virtual void OnClosed(PaneClosedEventArgs e)
    {
        Closed?.Invoke(this, e);
    }


    private void UpdateGuidePanel()
    {
        if (_defaultGuidePanel == null) return;

        // Hide default panel if custom content exists
        bool hasCustomContent = SplitGuideContent != null;
        _defaultGuidePanel.Visibility = hasCustomContent ? Visibility.Collapsed : Visibility.Visible;
    }

    private void UpdateGuideLines(Point mousePosition)
    {
        if (_verticalGuideLine == null || _horizontalGuideLine == null || _splitOverlay == null) 
        {
            return;
        }

        var width = _splitOverlay.ActualWidth;
        var height = _splitOverlay.ActualHeight;
        // Edge area size
        const double edgeThreshold = 24;
        
        // Check if hovering over guide areas
        bool isLeftEdge = mousePosition.X <= edgeThreshold;
        bool isRightEdge = mousePosition.X >= width - edgeThreshold;
        bool isTopEdge = mousePosition.Y <= edgeThreshold;
        bool isBottomEdge = mousePosition.Y >= height - edgeThreshold;

        // Always show guide lines based on mouse position
        // The split direction determines which line type to show
        if (_currentSplitDirection)
        {
            // Vertical split mode - show vertical line at mouse X
            ShowVerticalGuideLine(mousePosition.X, width, 0.8);
            HideHorizontalGuideLine();
        }
        else
        {
            // Horizontal split mode - show horizontal line at mouse Y
            ShowHorizontalGuideLine(mousePosition.Y, height, 0.8);
            HideVerticalGuideLine();
        }

        // Visual feedback: make lines slightly dimmer when over toggle areas
        if (isLeftEdge || isRightEdge || isTopEdge || isBottomEdge)
        {
            // Dim the lines slightly to indicate toggle area
            if (_verticalGuideLine != null && _verticalGuideLine.Opacity > 0)
                _verticalGuideLine.Opacity = 0.4;
            if (_horizontalGuideLine != null && _horizontalGuideLine.Opacity > 0)
                _horizontalGuideLine.Opacity = 0.4;
        }
    }

    private void ShowVerticalGuideLine(double x, double width, double opacity = 0.8)
    {
        if (_verticalGuideLine == null) 
        {
            return;
        }

        // Position vertical rectangle at mouse X position
        _verticalGuideLine.Margin = new Thickness(x - 1.5, 0, 0, 0); // Center the 3px wide line on mouse
        _verticalGuideLine.Opacity = opacity;
    }

    private void ShowHorizontalGuideLine(double y, double height, double opacity = 0.8)
    {
        if (_horizontalGuideLine == null) 
        {
            return;
        }

        // Position horizontal rectangle at mouse Y position
        _horizontalGuideLine.Margin = new Thickness(0, y - 1.5, 0, 0); // Center the 3px high line on mouse
        _horizontalGuideLine.Opacity = opacity;
    }

    private void HideGuideLines()
    {
        HideVerticalGuideLine();
        HideHorizontalGuideLine();
    }

    private void HideVerticalGuideLine()
    {
        if (_verticalGuideLine != null)
            _verticalGuideLine.Opacity = 0;
    }

    private void HideHorizontalGuideLine()
    {
        if (_horizontalGuideLine != null)
            _horizontalGuideLine.Opacity = 0;
    }

    private void UpdateSplitModeGuideText()
    {
        // Update the guide panel text to reflect current split direction
        if (_defaultGuidePanel != null && _defaultGuidePanel.Child is StackPanel stack)
        {
            // Find and update the direction indicator text blocks
            foreach (var child in stack.Children)
            {
                if (child is TextBlock textBlock)
                {
                    if (_currentSplitDirection)
                    {
                        // Vertical split mode
                        if (textBlock.Text.Contains("Current:"))
                        {
                            textBlock.Text = "Current: VERTICAL SPLIT (|)";
                            textBlock.Foreground = new SolidColorBrush(Color.FromRgb(0, 122, 204));
                        }
                    }
                    else
                    {
                        // Horizontal split mode  
                        if (textBlock.Text.Contains("Current:"))
                        {
                            textBlock.Text = "Current: HORIZONTAL SPLIT (—)";
                            textBlock.Foreground = new SolidColorBrush(Color.FromRgb(0, 122, 204));
                        }
                    }
                }
            }
        }
    }

    #endregion

    #region IDisposable Implementation

    public void Dispose()
    {
        if (_isDisposed) return;

        try
        {
            _isDisposed = true;

            // Disconnect FlexiPaneManager events first
            Managers.FlexiPaneManager.DisconnectPaneEvents(this);

            // Disconnect event handlers
            RemoveEventHandlers();
            this.Loaded -= OnLoaded;
            this.Unloaded -= OnUnloaded;

            // Clean up commands to prevent memory leaks
            if (CloseCommand is RelayCommand closeCmd)
                closeCmd.RaiseCanExecuteChanged();
            if (SplitVerticalCommand is RelayCommand vertCmd)
                vertCmd.RaiseCanExecuteChanged();
            if (SplitHorizontalCommand is RelayCommand horzCmd)
                horzCmd.RaiseCanExecuteChanged();

            // Clean up template references
            _containerGrid = null;
            _contentBorder = null;
            _splitOverlay = null;
            _closeButton = null;
            _verticalGuideLine = null;
            _horizontalGuideLine = null;
            _defaultGuidePanel = null;

            // Clear content to break potential circular references
            Content = null;
            
            // Force garbage collection of weak references
            GC.Collect(0, GCCollectionMode.Optimized);
        }
        catch (Exception)
        {
        }
    }

    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Force update the current split direction (used when Space key is pressed)
    /// </summary>
    internal void UpdateCurrentSplitDirection(bool newDirection)
    {
        if (_currentSplitDirection != newDirection)
        {
            _currentSplitDirection = newDirection;
            UpdateSplitModeGuideText();
        }
    }
    
    #endregion
}