using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
            
            // Subscribe to FlexiPanel events - use AddHandler to process PaneSplitRequested first
            FlexiPanel.AddHandler(FlexiPanel.PaneSplitRequestedEvent, 
                new PaneSplitRequestedEventHandler(OnFlexiPanelPaneSplitRequested), false);
            
            // Subscribe to other events normally
            FlexiPanel.LastPaneClosing += OnLastPaneClosing;
            FlexiPanel.PaneClosing += OnFlexiPanelPaneClosing;
            FlexiPanel.NewPaneCreated += OnNewPaneCreated;
            FlexiPanel.SplitModeChanged += OnSplitModeChanged;
            
            // Check Visual Tree state on startup
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
            // Manage global state through FlexiPanel binding
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
            
            // Provide custom content for new panel
            e.NewContent = CreateNewPaneContent();
            
            // Can cancel split under certain conditions
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
            // Can cancel close based on certain conditions here
            // Example: When there is unsaved data
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
            // Attempting to close last panel - confirm with user
            var result = MessageBox.Show(
                "This is the last remaining panel. Closing it will clear all content.\nDo you want to continue?", 
                "Confirm Last Panel Close", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.No)
            {
                e.Cancel = true; // Cancel close
            }
            // If Yes is selected, e.Cancel = false (default) so the panel closes
        }

        private void OnNewPaneCreated(object? sender, NewPaneCreatedEventArgs e)
        {
#if DEBUG
            Debug.WriteLine($"[MainWindow] New pane created - NewPane: {e.NewPane?.GetHashCode()}, SourcePane: {e.SourcePane?.GetHashCode()}");
#endif
            // Additional settings for newly created panel
            // e.g., connect event handlers, initialization, etc.
            if (e.NewPane != null)
            {
                // Increment panel counter (for UI update)
                UpdateUI();
            }
        }

        private void OnSplitModeChanged(object? sender, SplitModeChangedEventArgs e)
        {
#if DEBUG
            Debug.WriteLine($"[MainWindow] Split mode changed - IsActive: {e.IsActive}, OldValue: {e.OldValue}");
#endif
            // Additional processing for split mode changes
            // e.g., UI updates, toolbar enable/disable, etc.
            
            // Can cancel the change under certain conditions
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
            
            // Test with red background
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
            // Generate random color
            var r = _random.Next(0, 256);
            var g = _random.Next(0, 256);
            var b = _random.Next(0, 256);
            return new SolidColorBrush(Color.FromRgb((byte)r, (byte)g, (byte)b));
        }

        // HandlePaneSplit method is no longer needed - FlexiPaneManager handles it automatically

        private void UpdateUI()
        {
            var activePane = FlexiPanel.SelectedItem;

            SplitVerticalButton.IsEnabled = activePane?.CanSplit == true;
            SplitHorizontalButton.IsEnabled = activePane?.CanSplit == true;
            
            // Set button text based on FlexiPanel state
            ToggleSplitModeButton.Content = FlexiPanel.IsSplitModeActive ? "Split Mode OFF" : "Split Mode ON";
        }

        #endregion
    }
}