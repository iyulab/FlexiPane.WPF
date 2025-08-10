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
        
        // 디버그: Toggle 버튼과 FlexiPanel 상태 확인
        ModeToggleButton.Checked += (s, e) => {
            System.Diagnostics.Debug.WriteLine("🔛 Toggle ON - IsChecked: " + ModeToggleButton.IsChecked);
            System.Diagnostics.Debug.WriteLine("🔛 FlexiPanel.IsSplitModeActive BEFORE: " + FlexiPanel.IsSplitModeActive);
            
            // 바인딩이 작동하지 않으므로 직접 설정
            System.Diagnostics.Debug.WriteLine("🔛 Setting IsSplitModeActive directly to TRUE...");
            FlexiPanel.IsSplitModeActive = true;
            
            System.Diagnostics.Debug.WriteLine("🔛 FlexiPanel.IsSplitModeActive AFTER: " + FlexiPanel.IsSplitModeActive);
            StatusText.Text = "Split Mode: ON";
        };
        ModeToggleButton.Unchecked += (s, e) => {
            System.Diagnostics.Debug.WriteLine("🔲 Toggle OFF - IsChecked: " + ModeToggleButton.IsChecked);
            System.Diagnostics.Debug.WriteLine("🔲 FlexiPanel.IsSplitModeActive BEFORE: " + FlexiPanel.IsSplitModeActive);
            
            // 바인딩이 작동하지 않으므로 직접 설정  
            System.Diagnostics.Debug.WriteLine("🔲 Setting IsSplitModeActive directly to FALSE...");
            FlexiPanel.IsSplitModeActive = false;
            
            System.Diagnostics.Debug.WriteLine("🔲 FlexiPanel.IsSplitModeActive AFTER: " + FlexiPanel.IsSplitModeActive);
            StatusText.Text = "Split Mode: OFF";
        };
        
        // FlexiPanel의 Split Mode 변경 이벤트도 확인
        FlexiPanel.SplitModeChanged += (s, e) => {
            System.Diagnostics.Debug.WriteLine("⚡ SplitModeChanged - IsActive: " + e.IsActive);
            StatusText.Text = $"Split Mode: {(e.IsActive ? "ON" : "OFF")}";
        };
        
        // FlexiPanel 로드 완료 확인
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
        System.Diagnostics.Debug.WriteLine($"🎯 OnContentRequested CALLED! Purpose: {e.Purpose}");
        
        // 초기 콘텐츠, 초기 패널이나 새 패널을 위한 컨텐츠 생성
        if (e.Purpose == "InitialContent" || e.Purpose == "InitialPane" || e.RequestedContent == null)
        {
            // CreateNewContent()에서 Border를 반환 - FlexiPanel에서 FlexiPaneItem으로 래핑할 것
            e.RequestedContent = CreateNewContent();
            e.Handled = true;
            System.Diagnostics.Debug.WriteLine($"   - CreateNewContent() called for {e.Purpose}! Returned Type: {e.RequestedContent?.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"   - FlexiPanel will wrap this in FlexiPaneItem for splitting capability");
        }
    }

    private void OnPaneSplitRequested(object? sender, PaneSplitRequestedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("🔥 OnPaneSplitRequested CALLED!");
        System.Diagnostics.Debug.WriteLine($"   - IsVertical: {e.IsVerticalSplit}, Ratio: {e.SplitRatio}");
        System.Diagnostics.Debug.WriteLine($"   - NewContent is null: {e.NewContent == null}");
        
        // 새 패널을 위한 컨텐츠가 아직 없다면 CreateNewContent() 호출
        if (e.NewContent == null)
        {
            e.NewContent = CreateNewContent();
            System.Diagnostics.Debug.WriteLine("   - CreateNewContent() called, NewContent set");
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