using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FlexiPane.Controls;
using FlexiPane.Events;

namespace FlexiPane.Samples.DefaultApp;

public partial class MainWindowSimple : Window
{
    public MainWindowSimple()
    {
        InitializeComponent();
        
        // ToggleButton 상태 변경 로깅 및 수동 설정 추가
        ModeToggleButton.Checked += (s, e) => {
            System.Diagnostics.Debug.WriteLine($"🔘 ModeToggleButton CHECKED - IsChecked: {ModeToggleButton.IsChecked}");
            System.Diagnostics.Debug.WriteLine($"   - FlexiPanel.IsSplitModeActive BEFORE: {FlexiPanel.IsSplitModeActive}");
            
            // 바인딩이 실패하는 경우를 대비해 수동으로 설정
            FlexiPanel.IsSplitModeActive = true;
            
            System.Diagnostics.Debug.WriteLine($"   - FlexiPanel.IsSplitModeActive AFTER: {FlexiPanel.IsSplitModeActive}");
            
        };
        
        ModeToggleButton.Unchecked += (s, e) => {
            System.Diagnostics.Debug.WriteLine($"🔲 ModeToggleButton UNCHECKED - IsChecked: {ModeToggleButton.IsChecked}");
            System.Diagnostics.Debug.WriteLine($"   - FlexiPanel.IsSplitModeActive BEFORE: {FlexiPanel.IsSplitModeActive}");
            
            // 바인딩이 실패하는 경우를 대비해 수동으로 설정
            FlexiPanel.IsSplitModeActive = false;
            
            System.Diagnostics.Debug.WriteLine($"   - FlexiPanel.IsSplitModeActive AFTER: {FlexiPanel.IsSplitModeActive}");
            
        };
        
        // FlexiPanel IsSplitModeActive 속성 변경도 직접 모니터링
        var descriptor = System.ComponentModel.DependencyPropertyDescriptor.FromProperty(
            FlexiPane.Controls.FlexiPanel.IsSplitModeActiveProperty, 
            typeof(FlexiPane.Controls.FlexiPanel));
        descriptor?.AddValueChanged(FlexiPanel, (s, e) => {
            System.Diagnostics.Debug.WriteLine($"🎛️ FlexiPanel.IsSplitModeActive changed to: {FlexiPanel.IsSplitModeActive}");
        });
        
        // FlexiPanel의 Split Mode 변경 이벤트만 간단히 상태 표시용으로 사용
        FlexiPanel.SplitModeChanged += (s, e) => {
            System.Diagnostics.Debug.WriteLine("⚡ SplitModeChanged - IsActive: " + e.IsActive);
            StatusText.Text = $"Split Mode: {(e.IsActive ? "ON" : "OFF")}";
        };
        
        // FlexiPanel 로드 완료 확인 (디버깅용)
        FlexiPanel.Loaded += (s, e) => {
            System.Diagnostics.Debug.WriteLine("📋 FlexiPanel Loaded");
            System.Diagnostics.Debug.WriteLine($"📋 RootContent: {FlexiPanel.RootContent?.GetType().Name ?? "null"}");
            System.Diagnostics.Debug.WriteLine($"📋 SelectedItem: {FlexiPanel.SelectedItem?.GetType().Name ?? "null"}");
            System.Diagnostics.Debug.WriteLine($"📋 IsSplitModeActive: {FlexiPanel.IsSplitModeActive}");
            System.Diagnostics.Debug.WriteLine($"📋 ModeToggleButton.IsChecked: {ModeToggleButton.IsChecked}");
            
            // RootContent의 실제 내용도 확인
            if (FlexiPanel.RootContent is FlexiPane.Controls.FlexiPaneItem paneItem)
            {
                System.Diagnostics.Debug.WriteLine($"📋 FlexiPaneItem - Title: {paneItem.Title}, CanSplit: {paneItem.CanSplit}");
                System.Diagnostics.Debug.WriteLine($"📋 FlexiPaneItem - CanSplit: {paneItem.CanSplit}");
                System.Diagnostics.Debug.WriteLine($"📋 FlexiPaneItem Content: {paneItem.Content?.GetType().Name ?? "null"}");
            }
            else if (FlexiPanel.RootContent is System.Windows.Controls.Border border)
            {
                System.Diagnostics.Debug.WriteLine($"📋 Border Content: {border.Child?.GetType().Name ?? "null"}");
                if (border.Child is System.Windows.Controls.TextBlock textBlock)
                {
                    System.Diagnostics.Debug.WriteLine($"📋 TextBlock Text: {textBlock.Text}");
                }
            }
        };
    }

    private void OnContentRequested(object? sender, FlexiPane.Events.ContentRequestedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"🎯 OnContentRequested CALLED! RequestType: {e.RequestType}, Purpose: {e.Purpose}");
        
        if (e.RequestType == FlexiPane.Events.ContentRequestType.SplitPane)
        {
            System.Diagnostics.Debug.WriteLine($"   - Split request - IsVertical: {e.IsVerticalSplit}, Ratio: {e.SplitRatio}");
        }
        
        // 초기 콘텐츠, 초기 패널이나 새 패널을 위한 콘텐츠 생성
        if (e.RequestedContent == null)
        {
            // CreateNewContent()에서 Border를 반환 - FlexiPanel에서 FlexiPaneItem으로 래핑할 것
            e.RequestedContent = CreateNewContent();
            // 중요: SplitPane 요청의 경우 Handled를 true로 설정하지 않음
            // FlexiPanel이 추가 처리를 수행해야 함
            if (e.RequestType != FlexiPane.Events.ContentRequestType.SplitPane)
            {
                e.Handled = true;
            }
            System.Diagnostics.Debug.WriteLine($"   - CreateNewContent() called for {e.RequestType}/{e.Purpose}! Returned Type: {e.RequestedContent?.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"   - Handled set to: {e.Handled} (SplitPane requests need FlexiPanel processing)");
        }
    }


    public object CreateNewContent()
    {
        var border = new Border()
        {
            Background = GenerateRandomBrush(),
            Padding = new Thickness(10)
        };

        var textBlock = new TextBlock()
        {
            Text = $"Panel {DateTime.Now:HH:mm:ss}",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 14,
            Foreground = Brushes.White
        };

        border.Child = textBlock;
        
        // 단순히 Border를 반환 - FlexiPanel에서 필요시 FlexiPaneItem으로 래핑할 것
        return border;
    }


    private Brush GenerateRandomBrush()
    {
        var random = new Random();
        return new SolidColorBrush(Color.FromArgb(
            255,
            (byte)random.Next(0, 256),
            (byte)random.Next(0, 256),
            (byte)random.Next(0, 256)));
    }
}