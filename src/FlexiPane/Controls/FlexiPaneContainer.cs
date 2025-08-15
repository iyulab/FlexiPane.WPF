using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FlexiPane.Controls;

/// <summary>
/// Split container that holds two children
/// Manages splitting using Grid + GridSplitter
/// </summary>
[TemplatePart(Name = "PART_ContainerGrid", Type = typeof(Grid))]
public class FlexiPaneContainer : Control, IDisposable
{
    static FlexiPaneContainer()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(FlexiPaneContainer), 
            new FrameworkPropertyMetadata(typeof(FlexiPaneContainer)));
    }

    private Grid? _containerGrid;
    private bool _isDisposed = false;
    private bool _isVerticalSplit = true;
    private double _splitRatio = 0.5;
    private double _splitterThickness = 6.0;
    private bool _isApplyingTemplate = false;

    #region Dependency Properties

    /// <summary>
    /// First child (left or top)
    /// </summary>
    public UIElement FirstChild
    {
        get { return (UIElement)GetValue(FirstChildProperty); }
        set { SetValue(FirstChildProperty, value); }
    }

    public static readonly DependencyProperty FirstChildProperty =
        DependencyProperty.Register(nameof(FirstChild), typeof(UIElement), typeof(FlexiPaneContainer), 
            new PropertyMetadata(null, OnChildChanged));

    /// <summary>
    /// Second child (right or bottom)
    /// </summary>
    public UIElement SecondChild
    {
        get { return (UIElement)GetValue(SecondChildProperty); }
        set { SetValue(SecondChildProperty, value); }
    }

    public static readonly DependencyProperty SecondChildProperty =
        DependencyProperty.Register(nameof(SecondChild), typeof(UIElement), typeof(FlexiPaneContainer), 
            new PropertyMetadata(null, OnChildChanged));

    /// <summary>
    /// Split direction (true: vertical split/left-right, false: horizontal split/top-bottom)
    /// </summary>
    public bool IsVerticalSplit
    {
        get { return (bool)GetValue(IsVerticalSplitProperty); }
        set { SetValue(IsVerticalSplitProperty, value); }
    }

    public static readonly DependencyProperty IsVerticalSplitProperty =
        DependencyProperty.Register(nameof(IsVerticalSplit), typeof(bool), typeof(FlexiPaneContainer), 
            new PropertyMetadata(true, OnLayoutPropertyChanged));

    /// <summary>
    /// Split ratio (0.1 ~ 0.9)
    /// </summary>
    public double SplitRatio
    {
        get { return (double)GetValue(SplitRatioProperty); }
        set { SetValue(SplitRatioProperty, value); }
    }

    public static readonly DependencyProperty SplitRatioProperty =
        DependencyProperty.Register(nameof(SplitRatio), typeof(double), typeof(FlexiPaneContainer), 
            new PropertyMetadata(0.5, OnLayoutPropertyChanged, CoerceSplitRatio));

    /// <summary>
    /// GridSplitter thickness
    /// </summary>
    public double SplitterThickness
    {
        get { return (double)GetValue(SplitterThicknessProperty); }
        set { SetValue(SplitterThicknessProperty, value); }
    }

    public static readonly DependencyProperty SplitterThicknessProperty =
        DependencyProperty.Register(nameof(SplitterThickness), typeof(double), typeof(FlexiPaneContainer),
            new PropertyMetadata(6.0, OnLayoutPropertyChanged));


    #endregion

    #region Property Changed Callbacks

    private static void OnChildChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FlexiPaneContainer container && !container._isDisposed)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[FlexiPaneContainer] OnChildChanged - Property: {e.Property.Name}, OldValue: {e.OldValue?.GetType().Name}, NewValue: {e.NewValue?.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"[FlexiPaneContainer] Template applied: {container._containerGrid != null}");
#endif
            
            // Force immediate template application if not ready
            if (container._containerGrid == null && !container._isApplyingTemplate)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FlexiPaneContainer] Template not ready - forcing immediate application");
#endif
                container.ApplyTemplate();
                
                // Force UI thread processing to ensure template is applied
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
                    System.Windows.Threading.DispatcherPriority.Render, 
                    new Action(() => { }));
            }
            
            // Always try to update layout
            container.UpdateLayout();
        }
    }

    private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FlexiPaneContainer container && !container._isDisposed)
        {
            container.UpdateLayout();
        }
    }

    private static object CoerceSplitRatio(DependencyObject d, object baseValue)
    {
        if (baseValue is double ratio)
        {
            return Math.Max(0.1, Math.Min(0.9, ratio));
        }
        return 0.5;
    }

    #endregion

    #region Template Handling

    public override void OnApplyTemplate()
    {
        _isApplyingTemplate = true;
        
        base.OnApplyTemplate();

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[FlexiPaneContainer] OnApplyTemplate START - Getting template grid");
#endif

        _containerGrid = GetTemplateChild("PART_ContainerGrid") as Grid;
        
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[FlexiPaneContainer] OnApplyTemplate COMPLETE - ContainerGrid: {_containerGrid != null}");
        if (_containerGrid != null)
        {
            System.Diagnostics.Debug.WriteLine($"[FlexiPaneContainer] ContainerGrid details - ActualWidth: {_containerGrid.ActualWidth}, ActualHeight: {_containerGrid.ActualHeight}");
        }
#endif
        
        _isApplyingTemplate = false;
        
        // Force immediate layout update after template is applied
        if (_containerGrid != null && FirstChild != null && SecondChild != null)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[FlexiPaneContainer] Template applied successfully - forcing immediate layout update");
#endif
            UpdateLayout();
        }
        else
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[FlexiPaneContainer] Template applied but conditions not met - Grid: {_containerGrid != null}, FirstChild: {FirstChild != null}, SecondChild: {SecondChild != null}");
#endif
        }
    }

    #endregion

    #region Layout Management

    /// <summary>
    /// Update container layout with performance optimization
    /// </summary>
    private new void UpdateLayout()
    {
        if (_containerGrid == null || _isDisposed || _isApplyingTemplate) 
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[FlexiPaneContainer] UpdateLayout skipped - Grid: {_containerGrid != null}, Disposed: {_isDisposed}, Applying: {_isApplyingTemplate}");
#endif
            return;
        }

        if (FirstChild == null || SecondChild == null) 
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[FlexiPaneContainer] UpdateLayout skipped - FirstChild: {FirstChild != null}, SecondChild: {SecondChild != null}");
#endif
            return;
        }

        // Performance optimization: Only update if layout properties changed or children are missing
        bool layoutChanged = _isVerticalSplit != IsVerticalSplit || 
                           Math.Abs(_splitRatio - SplitRatio) > 0.001 ||
                           Math.Abs(_splitterThickness - SplitterThickness) > 0.001;

        bool hasCorrectChildren = _containerGrid.Children.Count >= 2 && 
                                _containerGrid.Children.Contains(FirstChild) && 
                                _containerGrid.Children.Contains(SecondChild);

        if (!layoutChanged && hasCorrectChildren)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[FlexiPaneContainer] UpdateLayout skipped - no layout changes detected and children are correct");
#endif
            return; // Skip update if nothing changed and children are properly placed
        }

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[FlexiPaneContainer] UpdateLayout proceeding - layoutChanged: {layoutChanged}, children: {_containerGrid.Children.Count}");
#endif

        UpdateLayoutCore();
        
        // Cache current values for next comparison
        _isVerticalSplit = IsVerticalSplit;
        _splitRatio = SplitRatio;
        _splitterThickness = SplitterThickness;
    }

    /// <summary>
    /// Core layout update logic
    /// </summary>
    private void UpdateLayoutCore()
    {
        try
        {
            // Double check that grid is still available
            if (_containerGrid == null)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FlexiPaneContainer] UpdateLayoutCore - Grid became null, aborting");
#endif
                return;
            }

            // Only clear and rebuild if necessary
            _containerGrid.Children.Clear();
            _containerGrid.ColumnDefinitions.Clear();
            _containerGrid.RowDefinitions.Clear();

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[FlexiPaneContainer] UpdateLayoutCore - Setting up {(IsVerticalSplit ? "vertical" : "horizontal")} split");
#endif

            if (IsVerticalSplit)
            {
                SetupVerticalSplit();
            }
            else
            {
                SetupHorizontalSplit();
            }

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[FlexiPaneContainer] UpdateLayoutCore - Layout complete, children: {_containerGrid.Children.Count}");
#endif
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[FlexiPaneContainer] UpdateLayoutCore error: {ex.Message}");
#endif
        }
    }

    private void SetupVerticalSplit()
    {
        // Column definition: first | splitter | second
        _containerGrid!.ColumnDefinitions.Add(new ColumnDefinition 
        { 
            Width = new GridLength(SplitRatio, GridUnitType.Star) 
        });
        _containerGrid!.ColumnDefinitions.Add(new ColumnDefinition 
        { 
            Width = new GridLength(SplitterThickness, GridUnitType.Pixel) 
        });
        _containerGrid!.ColumnDefinitions.Add(new ColumnDefinition 
        { 
            Width = new GridLength(1 - SplitRatio, GridUnitType.Star) 
        });

        // First child
        Grid.SetColumn(FirstChild, 0);
        _containerGrid!.Children.Add(FirstChild);

        // GridSplitter
        var splitter = CreateGridSplitter(true);
        Grid.SetColumn(splitter, 1);
        _containerGrid!.Children.Add(splitter);

        // Second child
        Grid.SetColumn(SecondChild, 2);
        _containerGrid!.Children.Add(SecondChild);
    }

    private void SetupHorizontalSplit()
    {
        // Row definition: first | splitter | second
        _containerGrid!.RowDefinitions.Add(new RowDefinition 
        { 
            Height = new GridLength(SplitRatio, GridUnitType.Star) 
        });
        _containerGrid!.RowDefinitions.Add(new RowDefinition 
        { 
            Height = new GridLength(SplitterThickness, GridUnitType.Pixel) 
        });
        _containerGrid!.RowDefinitions.Add(new RowDefinition 
        { 
            Height = new GridLength(1 - SplitRatio, GridUnitType.Star) 
        });

        // First child
        Grid.SetRow(FirstChild, 0);
        _containerGrid!.Children.Add(FirstChild);

        // GridSplitter
        var splitter = CreateGridSplitter(false);
        Grid.SetRow(splitter, 1);
        _containerGrid!.Children.Add(splitter);

        // Second child
        Grid.SetRow(SecondChild, 2);
        _containerGrid!.Children.Add(SecondChild);
    }

    private GridSplitter CreateGridSplitter(bool isVertical)
    {
        var splitter = new GridSplitter
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Background = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
            ShowsPreview = true,
            ToolTip = "Drag to resize split"
        };

        if (isVertical)
        {
            splitter.BorderThickness = new Thickness(1, 0, 1, 0);
            splitter.Cursor = Cursors.SizeWE;
            splitter.ResizeDirection = GridResizeDirection.Columns;
            splitter.ResizeBehavior = GridResizeBehavior.PreviousAndNext;
        }
        else
        {
            splitter.BorderThickness = new Thickness(0, 1, 0, 1);
            splitter.Cursor = Cursors.SizeNS;
            splitter.ResizeDirection = GridResizeDirection.Rows;
            splitter.ResizeBehavior = GridResizeBehavior.PreviousAndNext;
        }

        return splitter;
    }

    #endregion

    #region IDisposable Implementation

    public void Dispose()
    {
        if (_isDisposed) return;

        try
        {
            _isDisposed = true;
            
            // Dispose child elements if they implement IDisposable
            if (FirstChild is IDisposable firstDisposable)
                firstDisposable.Dispose();
            if (SecondChild is IDisposable secondDisposable)
                secondDisposable.Dispose();
            
            // Clean up grid elements
            if (_containerGrid != null)
            {
                _containerGrid.Children.Clear();
                _containerGrid.ColumnDefinitions.Clear();
                _containerGrid.RowDefinitions.Clear();
            }

            _containerGrid = null;
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"FlexiPaneContainer Dispose error: {ex.Message}");
#endif
        }
    }

    #endregion
}