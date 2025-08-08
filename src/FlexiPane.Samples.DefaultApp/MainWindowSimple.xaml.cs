using System.Windows;

namespace FlexiPane.Samples.DefaultApp
{
    /// <summary>
    /// Simple example showing minimal FlexiPanel usage
    /// </summary>
    public partial class MainWindowSimple : Window
    {
        public MainWindowSimple()
        {
            InitializeComponent();
        }

        private void ToggleSplitMode_Click(object sender, RoutedEventArgs e)
        {
            // Toggle split mode
            FlexiPanel.IsSplitModeActive = !FlexiPanel.IsSplitModeActive;
            FlexiPanel.ShowCloseButtons = FlexiPanel.IsSplitModeActive;
        }

        private void SplitVertical_Click(object sender, RoutedEventArgs e)
        {
            // Split selected pane vertically using built-in method
            FlexiPanel.SplitSelectedVertically();
        }

        private void SplitHorizontal_Click(object sender, RoutedEventArgs e)
        {
            // Split selected pane horizontally using built-in method
            FlexiPanel.SplitSelectedHorizontally();
        }
    }
}