using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FlexiPane.Events;

namespace FlexiPane.Controls
{
    /// <summary>
    /// Top-level container and global state manager for FlexiPane system
    /// Uses WPF Attached Property pattern to inherit state to child elements
    /// </summary>
    [TemplatePart(Name = "PART_RootContent", Type = typeof(ContentPresenter))]
    public class FlexiPanel : Control
    {
        static FlexiPanel()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FlexiPanel), 
                new FrameworkPropertyMetadata(typeof(FlexiPanel)));
        }
        
        public FlexiPanel()
        {
            // Add instance event handlers
            AddHandler(PaneSplitRequestedEvent, new PaneSplitRequestedEventHandler(OnPaneSplitRequested), true);
            AddHandler(PaneClosingEvent, new PaneClosingEventHandler(OnPaneClosing), true);
            
            // Initialize with default content if not provided
            Loaded += OnLoaded;
        }
        
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // If no content is provided, create a default pane
            if (RootContent == null)
            {
                var defaultPane = new FlexiPaneItem
                {
                    Title = "Main Panel",
                    CanSplit = true,
                    Content = CreateDefaultContent()
                };
                RootContent = defaultPane;
            }
            
            // Select the first pane if none is selected
            if (SelectedItem == null)
            {
                var firstPane = FindFirstSelectablePane();
                if (firstPane != null)
                {
                    firstPane.IsSelected = true;
                }
            }
        }
        
        private UIElement CreateDefaultContent()
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                Padding = new Thickness(20)
            };
            
            var textBlock = new TextBlock
            {
                Text = "FlexiPane - Dynamic Panel Splitter\n\nEnable split mode to start splitting panels.",
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100))
            };
            
            border.Child = textBlock;
            return border;
        }

        #region Attached Properties (Inheritable)

        /// <summary>
        /// Split mode activation state (auto-inherited)
        /// </summary>
        public static readonly DependencyProperty IsSplitModeActiveProperty =
            DependencyProperty.RegisterAttached("IsSplitModeActive", typeof(bool), typeof(FlexiPanel),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

        public static void SetIsSplitModeActive(DependencyObject element, bool value)
        {
            element.SetValue(IsSplitModeActiveProperty, value);
        }

        public static bool GetIsSplitModeActive(DependencyObject element)
        {
            return (bool)element.GetValue(IsSplitModeActiveProperty);
        }

        /// <summary>
        /// Close button display state (auto-inherited)
        /// </summary>
        public static readonly DependencyProperty ShowCloseButtonsProperty =
            DependencyProperty.RegisterAttached("ShowCloseButtons", typeof(bool), typeof(FlexiPanel),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

        public static void SetShowCloseButtons(DependencyObject element, bool value)
        {
            element.SetValue(ShowCloseButtonsProperty, value);
        }

        public static bool GetShowCloseButtons(DependencyObject element)
        {
            return (bool)element.GetValue(ShowCloseButtonsProperty);
        }

        #endregion

        #region Instance Properties

        /// <summary>
        /// Global split mode state (instance property)
        /// </summary>
        public bool IsSplitModeActive
        {
            get { return (bool)GetValue(IsSplitModeActiveInstanceProperty); }
            set { SetValue(IsSplitModeActiveInstanceProperty, value); }
        }

        public static readonly DependencyProperty IsSplitModeActiveInstanceProperty =
            DependencyProperty.Register("IsSplitModeActiveInstance", typeof(bool), typeof(FlexiPanel),
                new PropertyMetadata(false, OnSplitModeChanged));

        /// <summary>
        /// Global close button display state (instance property)
        /// </summary>
        public bool ShowCloseButtons
        {
            get { return (bool)GetValue(ShowCloseButtonsInstanceProperty); }
            set { SetValue(ShowCloseButtonsInstanceProperty, value); }
        }

        public static readonly DependencyProperty ShowCloseButtonsInstanceProperty =
            DependencyProperty.Register("ShowCloseButtonsInstance", typeof(bool), typeof(FlexiPanel),
                new PropertyMetadata(false, OnShowCloseButtonsChanged));

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
                new PropertyMetadata(null));

        #endregion

        #region Property Changed Handlers

        private static void OnSplitModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlexiPanel panel)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FlexiPanel] Split mode changed: {e.NewValue}");
#endif
                
                // Raise event (cancellable)
                var args = new SplitModeChangedEventArgs((bool)e.NewValue, (bool)e.OldValue)
                {
                    RoutedEvent = SplitModeChangedEvent
                };
                panel.RaiseEvent(args);
                
                if (args.Cancel)
                {
                    // Cancel change - restore previous value
                    panel.IsSplitModeActive = (bool)e.OldValue;
                    return;
                }
                
                // Attached Property에 전파
                SetIsSplitModeActive(panel, (bool)e.NewValue);
                
                // RootContent와 하위 요소들에게도 전파
                if (panel.RootContent != null)
                {
                    PropagateAttachedPropertyRecursively(panel.RootContent, IsSplitModeActiveProperty, e.NewValue);
                }
            }
        }

        private static void OnShowCloseButtonsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlexiPanel panel)
            {
                // Attached Property에 전파
                SetShowCloseButtons(panel, (bool)e.NewValue);
                
                // RootContent와 하위 요소들에게도 전파
                if (panel.RootContent != null)
                {
                    PropagateAttachedPropertyRecursively(panel.RootContent, ShowCloseButtonsProperty, e.NewValue);
                }
            }
        }

        private static void OnRootContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlexiPanel panel && e.NewValue is UIElement newContent)
            {
                // 새 콘텐츠와 하위 요소들에 현재 상태 적용
                PropagateAttachedPropertyRecursively(newContent, IsSplitModeActiveProperty, panel.IsSplitModeActive);
                PropagateAttachedPropertyRecursively(newContent, ShowCloseButtonsProperty, panel.ShowCloseButtons);
            }
        }

        #endregion

        #region Routed Events

        /// <summary>
        /// 패널 분할 요청 이벤트
        /// </summary>
        public static readonly RoutedEvent PaneSplitRequestedEvent =
            EventManager.RegisterRoutedEvent("PaneSplitRequested", RoutingStrategy.Bubble,
                typeof(PaneSplitRequestedEventHandler), typeof(FlexiPanel));

        public event PaneSplitRequestedEventHandler PaneSplitRequested
        {
            add { AddHandler(PaneSplitRequestedEvent, value); }
            remove { RemoveHandler(PaneSplitRequestedEvent, value); }
        }

        /// <summary>
        /// 패널 닫기 이벤트
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
        /// 마지막 패널 닫기 이벤트
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
        /// 분할 모드 변경 이벤트
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
        /// 새 패널 생성 이벤트
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

        private void OnPaneSplitRequested(object? sender, PaneSplitRequestedEventArgs e)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[FlexiPanel] RECEIVED SPLIT REQUEST - IsVertical: {e.IsVerticalSplit}, Ratio: {e.SplitRatio:F2}");
            System.Diagnostics.Debug.WriteLine($"[FlexiPanel] NewContent already set: {e.NewContent != null} (Type: {e.NewContent?.GetType().Name ?? "null"})");
#endif
            
            // 이벤트가 이미 처리되었으면 건너뛰기
            if (e.Handled)
                return;
            
            // Dispatcher를 사용하여 다른 핸들러들이 먼저 실행될 수 있도록 지연 처리
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
            {
                // 이벤트가 취소되었거나 이미 처리되었으면 건너뛰기
                if (e.Cancel || e.Handled)
                    return;
                    
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FlexiPanel] DELAYED PROCESSING - NewContent type after other handlers: {e.NewContent?.GetType().Name ?? "null"}");
#endif
                
                // FlexiPaneManager의 분할 처리 메서드 호출
                if (e.SourcePane != null && !e.Cancel)
                {
                    var result = Managers.FlexiPaneManager.SplitPane(
                        e.SourcePane, 
                        e.IsVerticalSplit, 
                        e.SplitRatio, 
                        e.NewContent as System.Windows.UIElement);
                    
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

        private void OnPaneClosing(object? sender, PaneClosingEventArgs e)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[FlexiPanel] OnPaneClosing - Processing close request for pane");
#endif

            // FlexiPaneManager를 통한 실제 닫기 처리
            if (e.Pane != null)
            {
                // 먼저 총 패널 수 확인
                var totalPanes = CountTotalPanes();
                if (totalPanes <= 1)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[FlexiPanel] Last panel closing. Current count: {totalPanes}");
#endif
                    // 마지막 패널 닫기 이벤트 발생
                    var lastPaneArgs = new LastPaneClosingEventArgs(e.Pane, e.Reason)
                    {
                        RoutedEvent = LastPaneClosingEvent
                    };
                    RaiseEvent(lastPaneArgs);
                    
                    if (lastPaneArgs.Cancel)
                    {
                        // 외부에서 취소 요청
                        e.Cancel = true;
                        return;
                    }
                    
                    // 마지막 패널도 닫기 허용 (콘텐츠 제거)
                    RootContent = null!;
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[FlexiPanel] Last panel closed - RootContent cleared");
#endif
                    return;
                }

                // 패널이 2개 이상인 경우 닫기 처리
                var parentContainer = Managers.FlexiPaneManager.FindDirectParentContainer(e.Pane);
                if (parentContainer != null)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[FlexiPanel] Found parent container, attempting to close pane. Total panes: {totalPanes}");
#endif
                    var success = Managers.FlexiPaneManager.ClosePane(parentContainer, e.Pane);
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
                else
                {
                    // RootContent가 단일 FlexiPaneItem인 경우
                    if (RootContent == e.Pane)
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine($"[FlexiPanel] Single pane is RootContent - triggering LastPaneClosing");
#endif
                        // 마지막 패널 닫기 이벤트 발생
                        var lastPaneArgs = new LastPaneClosingEventArgs(e.Pane, e.Reason)
                        {
                            RoutedEvent = LastPaneClosingEvent
                        };
                        RaiseEvent(lastPaneArgs);
                        
                        if (lastPaneArgs.Cancel)
                        {
                            e.Cancel = true;
                            return;
                        }
                        
                        // 마지막 패널 닫기 허용
                        RootContent = null!;
#if DEBUG
                        System.Diagnostics.Debug.WriteLine($"[FlexiPanel] Last panel closed - RootContent cleared");
#endif
                    }
                    else
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine($"[FlexiPanel] No parent container found and not RootContent - cancelling");
#endif
                        e.Cancel = true;
                    }
                }
            }
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
        /// 전체 패널 개수 세기
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
        /// RootContent를 새 콘텐츠로 교체
        /// </summary>
        internal void UpdateRootContent(UIElement newContent)
        {
            RootContent = newContent;
        }

        /// <summary>
        /// Attached Property를 하위 요소들에게 재귀적으로 전파
        /// </summary>
        private static void PropagateAttachedPropertyRecursively(UIElement element, DependencyProperty property, object value)
        {
            if (element == null) return;
            
            // 현재 요소에 설정
            element.SetValue(property, value);

            // 하위 요소들에게 재귀적으로 전파
            switch (element)
            {
                case FlexiPaneContainer container:
                    if (container.FirstChild != null)
                        PropagateAttachedPropertyRecursively(container.FirstChild, property, value);
                    if (container.SecondChild != null)
                        PropagateAttachedPropertyRecursively(container.SecondChild, property, value);
                    break;

                case System.Windows.Controls.Panel panel:
                    foreach (UIElement child in panel.Children)
                    {
                        PropagateAttachedPropertyRecursively(child, property, value);
                    }
                    break;

                case System.Windows.Controls.ContentControl contentControl:
                    if (contentControl.Content is UIElement contentElement)
                    {
                        PropagateAttachedPropertyRecursively(contentElement, property, value);
                    }
                    break;

                case System.Windows.Controls.Border border:
                    if (border.Child != null)
                    {
                        PropagateAttachedPropertyRecursively(border.Child, property, value);
                    }
                    break;
            }
        }

        #endregion
    }
}