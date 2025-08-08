using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using FlexiPane.Commands;
using FlexiPane.Events;
using FlexiPane.Managers;

namespace FlexiPane.Controls
{
    /// <summary>
    /// 실제 콘텐츠를 담는 분할 가능한 패널
    /// 분할 모드 UI를 제공하고, 분할되면 FlexiPaneContainer로 교체됨
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
        private Line? _verticalGuideLine;
        private Line? _horizontalGuideLine;
        private Border? _defaultGuidePanel;

        public FlexiPaneItem()
        {
            PaneId = Guid.NewGuid().ToString();
            
            // Commands 초기화
            CloseCommand = new RelayCommand(ExecuteClose, CanExecuteClose);
            SplitVerticalCommand = new RelayCommand(_ => RequestSplit(true), _ => CanSplit);
            SplitHorizontalCommand = new RelayCommand(_ => RequestSplit(false), _ => CanSplit);

            // 기본값 설정
            CanSplit = true;
            
            // 이벤트 구독
            this.Loaded += OnLoaded;
            this.Unloaded += OnUnloaded;
        }

        #region Dependency Properties
        
        /// <summary>
        /// Indicates whether this pane is currently selected/focused
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
            if (d is FlexiPaneItem item && (bool)e.NewValue)
            {
                // Notify FlexiPanel about selection change
                var panel = FlexiPanel.FindAncestorPanel(item);
                if (panel != null)
                {
                    panel.SetSelectedItem(item);
                }
            }
        }

        /// <summary>
        /// 패널 고유 식별자
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
                new PropertyMetadata(true));

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
        /// 분할 요청 이벤트
        /// </summary>
        public event EventHandler<PaneSplitRequestedEventArgs>? SplitRequested;

        /// <summary>
        /// 패널 닫기 요청 이벤트 (취소 가능)
        /// </summary>
        public event EventHandler<PaneClosingEventArgs>? Closing;

        /// <summary>
        /// 패널 닫힘 완료 이벤트
        /// </summary>
        public event EventHandler<PaneClosedEventArgs>? Closed;

        #endregion

        #region Template Handling

        public override void OnApplyTemplate()
        {
            if (_isDisposed) return;

            base.OnApplyTemplate();

            // 기존 이벤트 핸들러 제거
            RemoveEventHandlers();

            // 템플릿 요소 가져오기
            _containerGrid = GetTemplateChild("PART_ContainerGrid") as Grid;
            _contentBorder = GetTemplateChild("PART_ContentBorder") as Border;
            _splitOverlay = GetTemplateChild("PART_SplitOverlay") as Grid;
            _closeButton = GetTemplateChild("PART_CloseButton") as Button;
            _verticalGuideLine = GetTemplateChild("PART_VerticalGuideLine") as Line;
            _horizontalGuideLine = GetTemplateChild("PART_HorizontalGuideLine") as Line;
            _defaultGuidePanel = GetTemplateChild("PART_DefaultGuidePanel") as Border;

            // 이벤트 핸들러 연결
            ConnectEventHandlers();

            // 키보드 포커스 설정
            this.Focusable = true;
        }

        private void ConnectEventHandlers()
        {
            if (_splitOverlay != null)
            {
                _splitOverlay.MouseLeftButtonUp += OnSplitOverlayClick;
                _splitOverlay.MouseMove += OnSplitOverlayMouseMove;
                _splitOverlay.MouseLeave += OnSplitOverlayMouseLeave;
            }

            this.KeyDown += OnKeyDown;
            
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
            }

            this.KeyDown -= OnKeyDown;
            this.GotFocus -= OnGotFocus;
            this.MouseDown -= OnMouseDown;
        }

        #endregion

        #region Event Handlers

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // 로드 시 FlexiPaneManager에 이벤트 자동 연결
            FlexiPaneManager.ConnectPaneEvents(this);
#if DEBUG
            Debug.WriteLine($"[FlexiPaneItem] OnLoaded - Connected to FlexiPaneManager");
#endif
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // 언로드 시 FlexiPaneManager에서 이벤트 해제
            FlexiPaneManager.DisconnectPaneEvents(this);
#if DEBUG
            Debug.WriteLine($"[FlexiPaneItem] OnUnloaded - Disconnected from FlexiPaneManager");
#endif
        }


        private void OnSplitOverlayClick(object sender, MouseButtonEventArgs e)
        {
            var isSplitModeActive = FlexiPanel.GetIsSplitModeActive(this);
            
#if DEBUG
            Debug.WriteLine($"[FlexiPaneItem] SPLIT CLICK - IsSplitModeActive: {isSplitModeActive}, CanSplit: {CanSplit}");
#endif

            if (!isSplitModeActive || !CanSplit)
            {
#if DEBUG
                Debug.WriteLine($"[FlexiPaneItem] SPLIT IGNORED");
#endif
                return;
            }

            // 클릭 위치에 따라 분할 방향 결정
            var position = e.GetPosition(_splitOverlay);
            var width = _splitOverlay!.ActualWidth;
            var height = _splitOverlay!.ActualHeight;

#if DEBUG
            Debug.WriteLine($"[FlexiPaneItem] Click position: ({position.X:F2}, {position.Y:F2}), Overlay size: {width:F2}x{height:F2}");
#endif

            bool isVerticalSplit;
            double splitRatio;

            // 가장자리 영역 크기 (UpdateGuideLines와 동일)
            const double edgeThreshold = 50;

            // 가장자리 클릭으로 분할 방향 결정
            if (position.X <= edgeThreshold || position.X >= width - edgeThreshold)
            {
                // 좌우 가장자리: 수평 분할 (가로선 위치에서 분할)
                isVerticalSplit = false;
                splitRatio = position.Y / height;
#if DEBUG
                Debug.WriteLine($"[FlexiPaneItem] Horizontal split - splitRatio: {splitRatio:F2}");
#endif
            }
            else
            {
                // 상하 또는 중앙: 수직 분할 (세로선 위치에서 분할)
                isVerticalSplit = true;
                splitRatio = position.X / width;
#if DEBUG
                Debug.WriteLine($"[FlexiPaneItem] Vertical split - splitRatio: {splitRatio:F2}");
#endif
            }

            // 분할 비율 범위 제한
            splitRatio = Math.Max(0.1, Math.Min(0.9, splitRatio));

#if DEBUG
            Debug.WriteLine($"[FlexiPaneItem] REQUESTING SPLIT - IsVertical: {isVerticalSplit}, Ratio: {splitRatio:F2}");
#endif

            RequestSplit(isVerticalSplit, splitRatio);
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
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
        }

        private void OnSplitOverlayMouseMove(object sender, MouseEventArgs e)
        {
            var isSplitModeActive = FlexiPanel.GetIsSplitModeActive(this);
            
            if (!isSplitModeActive || !CanSplit || _splitOverlay == null) return;

            UpdateGuideLines(e.GetPosition(_splitOverlay));
        }

        private void OnSplitOverlayMouseLeave(object sender, MouseEventArgs e)
        {
            HideGuideLines();
        }
        
        private void OnGotFocus(object sender, RoutedEventArgs e)
        {
            // Mark this pane as selected when it receives focus
            IsSelected = true;
#if DEBUG
            Debug.WriteLine($"[FlexiPaneItem] Got focus - IsSelected set to true");
#endif
        }
        
        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Select this pane on mouse click
            if (!IsSelected)
            {
                IsSelected = true;
                this.Focus();
#if DEBUG
                Debug.WriteLine($"[FlexiPaneItem] Mouse down - IsSelected set to true and focus set");
#endif
            }
        }

        #endregion

        #region Commands Implementation

        private void ExecuteClose(object? parameter)
        {
            var showCloseButtons = FlexiPanel.GetShowCloseButtons(this);
            
#if DEBUG
            Debug.WriteLine($"[FlexiPaneItem] ExecuteClose called - ShowCloseButtons: {showCloseButtons}, IsDisposed: {_isDisposed}");
#endif
            
            var args = new PaneClosingEventArgs(this, PaneCloseReason.UserRequest);
            OnClosing(args);

#if DEBUG
            Debug.WriteLine($"[FlexiPaneItem] After OnClosing - Cancel: {args.Cancel}");
#endif

            if (!args.Cancel)
            {
                OnClosed(new PaneClosedEventArgs(this, PaneCloseReason.UserRequest));
#if DEBUG
                Debug.WriteLine($"[FlexiPaneItem] OnClosed completed");
#endif
            }
        }

        private bool CanExecuteClose(object? parameter)
        {
            return FlexiPanel.GetShowCloseButtons(this) && !_isDisposed;
        }

        private void RequestSplit(bool isVerticalSplit, double splitRatio = 0.5)
        {
            if (!CanSplit || _isDisposed) 
            {
#if DEBUG
                Debug.WriteLine($"[FlexiPaneItem] SPLIT BLOCKED - CanSplit: {CanSplit}, IsDisposed: {_isDisposed}");
#endif
                return;
            }

#if DEBUG
            Debug.WriteLine($"[FlexiPaneItem] CREATING SPLIT EVENT - IsVertical: {isVerticalSplit}, Ratio: {splitRatio:F2}");
#endif

            var args = new PaneSplitRequestedEventArgs(this, isVerticalSplit, splitRatio);
            OnSplitRequested(args);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 패널 닫기
        /// </summary>
        public void Close()
        {
            if (CloseCommand?.CanExecute(null) == true)
            {
                CloseCommand.Execute(null);
            }
        }

        /// <summary>
        /// 패널 분할
        /// </summary>
        public void Split(bool isVerticalSplit, double splitRatio = 0.5, object? newContent = null)
        {
            if (!CanSplit) return;
            
            var args = new PaneSplitRequestedEventArgs(this, isVerticalSplit, splitRatio)
            {
                NewContent = newContent
            };
            OnSplitRequested(args);
        }


        #endregion

        #region Protected Methods

        protected virtual void OnSplitRequested(PaneSplitRequestedEventArgs e)
        {
#if DEBUG
            Debug.WriteLine($"[FlexiPaneItem] RAISING SPLIT EVENT - Routing to FlexiPanel");
#endif
            // FlexiPanel의 Routed Event로 버블링
            e.RoutedEvent = FlexiPanel.PaneSplitRequestedEvent;
            RaiseEvent(e);
        }

        protected virtual void OnClosing(PaneClosingEventArgs e)
        {
#if DEBUG
            Debug.WriteLine($"[FlexiPaneItem] OnClosing - Raising routed event");
#endif
            // FlexiPanel의 Routed Event로 버블링
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

            // 사용자 정의 콘텐츠가 있으면 기본 패널 숨기기
            bool hasCustomContent = SplitGuideContent != null;
            _defaultGuidePanel.Visibility = hasCustomContent ? Visibility.Collapsed : Visibility.Visible;
        }

        private void UpdateGuideLines(Point mousePosition)
        {
            if (_verticalGuideLine == null || _horizontalGuideLine == null || _splitOverlay == null) return;

            var width = _splitOverlay.ActualWidth;
            var height = _splitOverlay.ActualHeight;

            // 가장자리 영역 크기
            const double edgeThreshold = 50;
            
            // 마우스 위치에 따라 분할 방향 결정
            if (mousePosition.X <= edgeThreshold || mousePosition.X >= width - edgeThreshold)
            {
                // 좌우 가장자리: 수평 분할 (세로선 표시)
                ShowVerticalGuideLine(mousePosition.Y, height);
                HideHorizontalGuideLine();
            }
            else
            {
                // 상하 또는 중앙: 수직 분할 (가로선 표시)
                ShowHorizontalGuideLine(mousePosition.X, width);
                HideVerticalGuideLine();
            }
        }

        private void ShowVerticalGuideLine(double y, double height)
        {
            if (_verticalGuideLine == null) return;

            _verticalGuideLine.X1 = 0;
            _verticalGuideLine.Y1 = y;
            _verticalGuideLine.X2 = _splitOverlay?.ActualWidth ?? 0;
            _verticalGuideLine.Y2 = y;
            _verticalGuideLine.Opacity = 0.8;
        }

        private void ShowHorizontalGuideLine(double x, double width)
        {
            if (_horizontalGuideLine == null) return;

            _horizontalGuideLine.X1 = x;
            _horizontalGuideLine.Y1 = 0;
            _horizontalGuideLine.X2 = x;
            _horizontalGuideLine.Y2 = _splitOverlay?.ActualHeight ?? 0;
            _horizontalGuideLine.Opacity = 0.8;
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

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            if (_isDisposed) return;

            try
            {
                _isDisposed = true;

                // 이벤트 핸들러 해제
                RemoveEventHandlers();
                this.Loaded -= OnLoaded;
                this.Unloaded -= OnUnloaded;

                // 리소스 정리
                _containerGrid = null;
                _contentBorder = null;
                _splitOverlay = null;
                _closeButton = null;
                _verticalGuideLine = null;
                _horizontalGuideLine = null;
                _defaultGuidePanel = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FlexiPaneItem Dispose 오류: {ex.Message}");
            }
        }

        #endregion
    }
}