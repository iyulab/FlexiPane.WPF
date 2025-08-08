using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FlexiPane.Controls
{
    /// <summary>
    /// 2개의 자식을 가지는 분할 컨테이너
    /// Grid + GridSplitter로 분할하여 관리
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

        #region Dependency Properties

        /// <summary>
        /// 첫 번째 자식 (왼쪽 또는 상단)
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
        /// 두 번째 자식 (오른쪽 또는 하단)
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
        /// 분할 방향 (true: 수직분할/좌우, false: 수평분할/상하)
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
        /// 분할 비율 (0.1 ~ 0.9)
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
        /// GridSplitter 두께
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
            base.OnApplyTemplate();

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[FlexiPaneContainer] OnApplyTemplate - Getting template grid");
#endif

            _containerGrid = GetTemplateChild("PART_ContainerGrid") as Grid;
            
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[FlexiPaneContainer] Template applied - ContainerGrid: {_containerGrid != null}");
#endif
            
            UpdateLayout();
        }

        #endregion

        #region Layout Management

        /// <summary>
        /// 컨테이너 레이아웃 업데이트
        /// </summary>
        private new void UpdateLayout()
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[FlexiPaneContainer] UpdateLayout - ContainerGrid: {_containerGrid != null}, Disposed: {_isDisposed}");
            System.Diagnostics.Debug.WriteLine($"[FlexiPaneContainer] Children - First: {FirstChild?.GetType().Name}, Second: {SecondChild?.GetType().Name}");
#endif
            
            if (_containerGrid == null || _isDisposed) 
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FlexiPaneContainer] UpdateLayout SKIPPED - Grid null or disposed");
#endif
                return;
            }

            _containerGrid.Children.Clear();
            _containerGrid.ColumnDefinitions.Clear();
            _containerGrid.RowDefinitions.Clear();

            if (FirstChild == null || SecondChild == null) 
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FlexiPaneContainer] UpdateLayout SKIPPED - Missing children");
#endif
                return;
            }

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[FlexiPaneContainer] Setting up split - IsVertical: {IsVerticalSplit}, Ratio: {SplitRatio}");
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
            System.Diagnostics.Debug.WriteLine($"[FlexiPaneContainer] UpdateLayout COMPLETED - Grid children count: {_containerGrid.Children.Count}");
#endif
        }

        private void SetupVerticalSplit()
        {
            // 컬럼 정의: 첫 번째 | 스플리터 | 두 번째
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

            // 첫 번째 자식
            Grid.SetColumn(FirstChild, 0);
            _containerGrid!.Children.Add(FirstChild);

            // GridSplitter
            var splitter = CreateGridSplitter(true);
            Grid.SetColumn(splitter, 1);
            _containerGrid!.Children.Add(splitter);

            // 두 번째 자식
            Grid.SetColumn(SecondChild, 2);
            _containerGrid!.Children.Add(SecondChild);
        }

        private void SetupHorizontalSplit()
        {
            // 로우 정의: 첫 번째 | 스플리터 | 두 번째
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

            // 첫 번째 자식
            Grid.SetRow(FirstChild, 0);
            _containerGrid!.Children.Add(FirstChild);

            // GridSplitter
            var splitter = CreateGridSplitter(false);
            Grid.SetRow(splitter, 1);
            _containerGrid!.Children.Add(splitter);

            // 두 번째 자식
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
                ToolTip = "드래그하여 분할 크기 조정"
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
                
                // 자식 요소들 정리
                if (_containerGrid != null)
                {
                    _containerGrid.Children.Clear();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FlexiPaneContainer Dispose 오류: {ex.Message}");
            }
        }

        #endregion
    }
}