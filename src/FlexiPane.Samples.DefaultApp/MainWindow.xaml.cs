using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using FlexiPane.Controls;
using FlexiPane.Events;
using FlexiPane.Managers;

namespace FlexiPane.Samples.DefaultApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int _paneCounter = 1;
        private readonly Random _random = new();

        public MainWindow()
        {
            InitializeComponent();
            
#if DEBUG
            Debug.WriteLine($"[MainWindow] Initializing - subscribing to events");
#endif
            
            // FlexiPanel 이벤트 구독 - PaneSplitRequested를 먼저 처리하기 위해 AddHandler 사용
            FlexiPanel.AddHandler(FlexiPanel.PaneSplitRequestedEvent, 
                new PaneSplitRequestedEventHandler(OnFlexiPanelPaneSplitRequested), false);
            
            // 나머지 이벤트들은 일반 구독
            FlexiPanel.LastPaneClosing += OnLastPaneClosing;
            FlexiPanel.PaneClosing += OnFlexiPanelPaneClosing;
            FlexiPanel.NewPaneCreated += OnNewPaneCreated;
            FlexiPanel.SplitModeChanged += OnSplitModeChanged;
            
            // 시작시 Visual Tree 상태 확인
            this.Loaded += MainWindow_Loaded;
        }
        
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
#if DEBUG
            Debug.WriteLine($"[MainWindow] === INITIAL STATE CHECK ===");
            Debug.WriteLine($"[MainWindow] FlexiPanel IsSplitModeActive: {FlexiPanel.IsSplitModeActive}");
            Debug.WriteLine($"[MainWindow] FlexiPanel ShowCloseButtons: {FlexiPanel.ShowCloseButtons}");
            Debug.WriteLine($"[MainWindow] Total panes: {FlexiPanel.CountTotalPanes()}");
#endif
        }

        #region Event Handlers

        private void ToggleSplitModeButton_Click(object sender, RoutedEventArgs e)
        {
#if DEBUG
            Debug.WriteLine($"[MainWindow] ToggleSplitModeButton_Click");
#endif
            // FlexiPanel의 바인딩을 통해 전역 상태 관리
            FlexiPanel.IsSplitModeActive = !FlexiPanel.IsSplitModeActive;
            FlexiPanel.ShowCloseButtons = FlexiPanel.IsSplitModeActive;
            
#if DEBUG
            Debug.WriteLine($"[MainWindow] FlexiPanel split mode set to: {FlexiPanel.IsSplitModeActive}");
            Debug.WriteLine($"[MainWindow] FlexiPanel show close buttons set to: {FlexiPanel.ShowCloseButtons}");
#endif
            
            UpdateUI();
        }

        private void SplitVerticalButton_Click(object sender, RoutedEventArgs e)
        {
            // Use the built-in method to split the selected pane
            FlexiPanel.SplitSelectedVertically(0.5, CreateNewPaneContent());
        }

        private void SplitHorizontalButton_Click(object sender, RoutedEventArgs e)
        {
            // Use the built-in method to split the selected pane
            FlexiPanel.SplitSelectedHorizontally(0.5, CreateNewPaneContent());
        }

        private void OnFlexiPanelPaneSplitRequested(object? sender, PaneSplitRequestedEventArgs e)
        {
#if DEBUG
            Debug.WriteLine($"[MainWindow] FlexiPanel PaneSplitRequested - IsVertical: {e.IsVerticalSplit}, Ratio: {e.SplitRatio}");
#endif
            
            // 새 패널에 커스텀 콘텐츠 제공
            e.NewContent = CreateNewPaneContent();
            
            // 특정 조건에서 분할을 취소할 수도 있음
            // if (SomeCondition)
            // {
            //     e.Cancel = true;
            //     MessageBox.Show("Cannot split panel at this time.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
            // }
        }

        private void OnFlexiPanelPaneClosing(object? sender, PaneClosingEventArgs e)
        {
#if DEBUG
            Debug.WriteLine($"[MainWindow] FlexiPanel PaneClosing - Pane: {e.Pane?.GetHashCode()}");
#endif
            // 여기서 특정 조건에 따라 닫기를 취소할 수 있음
            // 예: 저장되지 않은 데이터가 있는 경우
            // if (HasUnsavedData(e.Pane))
            // {
            //     var result = MessageBox.Show("Unsaved data will be lost. Continue?", "Warning", 
            //         MessageBoxButton.YesNo, MessageBoxImage.Warning);
            //     if (result == MessageBoxResult.No)
            //         e.Cancel = true;
            // }
        }

        private void OnLastPaneClosing(object? sender, LastPaneClosingEventArgs e)
        {
#if DEBUG
            Debug.WriteLine($"[MainWindow] LastPaneClosing - Last pane: {e.Pane?.GetHashCode()}");
#endif
            // 마지막 패널 닫기 시도 - 사용자에게 확인
            var result = MessageBox.Show(
                "This is the last remaining panel. Closing it will clear all content.\nDo you want to continue?", 
                "Confirm Last Panel Close", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.No)
            {
                e.Cancel = true; // 닫기 취소
            }
            // Yes를 선택하면 e.Cancel = false (기본값)이므로 패널이 닫힘
        }

        private void OnNewPaneCreated(object? sender, NewPaneCreatedEventArgs e)
        {
#if DEBUG
            Debug.WriteLine($"[MainWindow] New pane created - NewPane: {e.NewPane?.GetHashCode()}, SourcePane: {e.SourcePane?.GetHashCode()}");
#endif
            // 새로 생성된 패널에 대한 추가 설정
            // 예: 이벤트 핸들러 연결, 초기화 등
            if (e.NewPane != null)
            {
                // 패널 카운터 증가 (UI 업데이트용)
                UpdateUI();
            }
        }

        private void OnSplitModeChanged(object? sender, SplitModeChangedEventArgs e)
        {
#if DEBUG
            Debug.WriteLine($"[MainWindow] Split mode changed - IsActive: {e.IsActive}, OldValue: {e.OldValue}");
#endif
            // 분할 모드 변경에 대한 추가 처리
            // 예: UI 업데이트, 도구 모음 활성화/비활성화 등
            
            // 특정 조건에서 변경을 취소할 수도 있음
            // if (HasUnsavedChanges && e.IsActive)
            // {
            //     var result = MessageBox.Show("Enabling split mode will reset the layout. Continue?", 
            //         "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
            //     if (result == MessageBoxResult.No)
            //         e.Cancel = true;
            // }
        }

        #endregion

        #region Helper Methods

        private UIElement CreateNewPaneContent()
        {
            _paneCounter++;
            
#if DEBUG
            Debug.WriteLine($"[MainWindow] CreateNewPaneContent called - Creating pane #{_paneCounter}");
#endif
            
            // 빨간색 배경으로 테스트
            var border = new Border
            {
                Background = GenerateRandomBrush(),
                BorderBrush = Brushes.DarkRed,
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(16)
            };

            var stackPanel = new StackPanel();

            var titleBlock = new TextBlock
            {
                Text = $"[New Pane #{_paneCounter} content]",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var contentBlock = new TextBlock
            {
                Text = $"Custom content from MainWindow\nCreated at: {DateTime.Now:HH:mm:ss}",
                FontSize = 11,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap
            };

            stackPanel.Children.Add(titleBlock);
            stackPanel.Children.Add(contentBlock);
            border.Child = stackPanel;

#if DEBUG
            Debug.WriteLine($"[MainWindow] CreateNewPaneContent returning Border with red background");
#endif

            return border;
        }

        private Brush GenerateRandomBrush()
        {
            // 랜덤 색상 생성
            var r = _random.Next(0, 256);
            var g = _random.Next(0, 256);
            var b = _random.Next(0, 256);
            return new SolidColorBrush(Color.FromRgb((byte)r, (byte)g, (byte)b));
        }

        // HandlePaneSplit 메서드도 더 이상 필요없음 - FlexiPaneManager가 자동 처리

        private void UpdateUI()
        {
            var activePane = FlexiPanel.SelectedItem;

            SplitVerticalButton.IsEnabled = activePane?.CanSplit == true;
            SplitHorizontalButton.IsEnabled = activePane?.CanSplit == true;
            
            // FlexiPanel 상태를 기반으로 버튼 텍스트 설정
            ToggleSplitModeButton.Content = FlexiPanel.IsSplitModeActive ? "Split Mode OFF" : "Split Mode ON";
        }

        #endregion
    }
}