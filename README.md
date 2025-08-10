# FlexiPane.WPF

[![NuGet Version](https://img.shields.io/nuget/v/FlexiPane.WPF)](https://www.nuget.org/packages/FlexiPane.WPF)
[![NuGet Downloads](https://img.shields.io/nuget/dt/FlexiPane.WPF)](https://www.nuget.org/packages/FlexiPane.WPF)
[![.NET Build and NuGet Publish](https://github.com/iyulab/FlexiPane.WPF/actions/workflows/dotnet.yml/badge.svg)](https://github.com/iyulab/FlexiPane.WPF/actions/workflows/dotnet.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A modern and flexible WPF screen splitting library for dynamic pane management. Create resizable, dockable panels with intuitive split controls and visual feedback.

<div align="center">
  <img src="images/Screenshot1.png" alt="FlexiPane Split Layout" width="49%" />
  <img src="images/Screenshot2.png" alt="FlexiPane Split Mode" width="49%" />
  
  *Left: Multiple panels with complex split layout | Right: Interactive split mode with visual guides*
</div>

## ✨ Features

- 🎯 **Dynamic Screen Splitting** - Split panels vertically or horizontally at runtime
- 🎨 **Toggle Split Mode** - Simple boolean property to enable/disable splitting functionality  
- 🔄 **Smart Panel Management** - Automatic structure preservation and recursive split control
- 🎮 **Programmatic Control** - Built-in methods for splitting selected panels
- 🎯 **Focus & Selection** - Automatic focus tracking and visual selection indicators
- 📐 **Flexible Layouts** - Support for complex nested split arrangements that persist
- 🔌 **Event-Driven Architecture** - Rich event system with ContentRequested and PaneSplitRequested
- 🎨 **Automatic Content Wrapping** - Any UIElement gets automatically wrapped for splitting capability
- ⚡ **High Performance** - Pure WPF implementation with optimized rendering
- 🛡️ **Safe Mode Toggling** - Split mode can be toggled on/off without losing existing layouts
- 🎛️ **Minimal Configuration** - Works with just `<flexiPane:FlexiPanel />` in XAML
- ⌨️ **Full Keyboard Support** - Complete keyboard navigation and shortcuts
- 💾 **Zero Configuration** - Works out of the box with sensible defaults and automatic content generation

## 📦 Installation

### Package Manager Console
```powershell
Install-Package FlexiPane.WPF
```

### .NET CLI
```bash
dotnet add package FlexiPane.WPF
```

### PackageReference
```xml
<PackageReference Include="FlexiPane.WPF" Version="1.0.0" />
```

## 🚀 Quick Start

Add the namespace to your XAML:

```xml
xmlns:flexiPane="clr-namespace:FlexiPane.Controls;assembly=FlexiPane"
```

### Simple Usage

Use the simplest possible configuration for basic split functionality:

```xml
<Grid>
  <Grid.RowDefinitions>
    <RowDefinition Height="Auto" />
    <RowDefinition Height="*" />
  </Grid.RowDefinitions>

  <!-- Control Panel -->
  <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="10">
    <ToggleButton x:Name="ModeToggleButton" Content="Toggle Split Mode" Padding="10,5" />
  </StackPanel>

  <!-- FlexiPanel with Event-Driven Content -->
  <flexiPane:FlexiPanel
    x:Name="FlexiPanel"
    Grid.Row="1"
    IsSplitModeActive="{Binding ElementName=ModeToggleButton, Path=IsChecked}"
    ContentRequested="OnContentRequested"
    PaneSplitRequested="OnPaneSplitRequested" />
</Grid>
```

### Event-Driven Content Generation

The panel uses an event-driven architecture for content creation:

```csharp
// Handle initial content creation
private void OnContentRequested(object sender, ContentRequestedEventArgs e)
{
    if (e.Purpose == "InitialContent" || e.Purpose == "InitialPane")
    {
        // Create your custom content - can be any UIElement
        e.RequestedContent = new Border
        {
            Background = Brushes.LightBlue,
            Child = new TextBlock
            {
                Text = $"Panel {DateTime.Now:HH:mm:ss}",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        e.Handled = true;
    }
}

// Handle split operations
private void OnPaneSplitRequested(object sender, PaneSplitRequestedEventArgs e)
{
    // Provide content for the new split panel
    if (e.NewContent == null)
    {
        e.NewContent = CreateNewPanelContent();
    }
}
```

### Programmatic Splitting

Split the selected panel programmatically:

```csharp
// Split vertically at 50%
flexiPanel.SplitSelectedVertically(0.5);

// Split horizontally with custom content
var customContent = new TextBlock { Text = "New Panel" };
flexiPanel.SplitSelectedHorizontally(0.3, customContent);
```

### Handling Panel Lifecycle Events

Customize behavior through comprehensive events:

```csharp
// Validate panel closing
flexiPanel.PaneClosing += (s, e) => {
    if (HasUnsavedChanges(e.Pane)) {
        e.Cancel = MessageBox.Show("Discard changes?", "Confirm", 
            MessageBoxButton.YesNo) == MessageBoxResult.No;
    }
};

// Handle last panel scenario
flexiPanel.LastPaneClosing += (s, e) => {
    e.Cancel = MessageBox.Show("Close last panel?", "Confirm",
        MessageBoxButton.YesNo) == MessageBoxResult.No;
};

// Track split mode changes
flexiPanel.SplitModeChanged += (s, e) => {
    statusLabel.Text = $"Split Mode: {(e.IsActive ? "ON" : "OFF")}";
};
```

## Architecture

### Core Components

- **FlexiPanel** - Root container managing the entire pane structure
- **FlexiPaneItem** - Individual panels containing user content
- **FlexiPaneContainer** - Split containers holding two children with GridSplitter
- **FlexiPaneManager** - Static manager handling split/remove operations

### Tree Structure

The library uses a binary tree structure for managing panels:

```
FlexiPanel
└── RootContent
    ├── FlexiPaneItem (leaf node)
    └── FlexiPaneContainer (branch node)
        ├── FirstChild
        └── SecondChild
```

## Advanced Features

### Split Mode UI

Enable interactive split mode with simple property binding:

```xml
<ToggleButton x:Name="SplitToggle" Content="Toggle Split Mode" />
<flexiPane:FlexiPanel IsSplitModeActive="{Binding ElementName=SplitToggle, Path=IsChecked}" />
```

Or programmatically:

```csharp
flexiPanel.IsSplitModeActive = true;
flexiPanel.ShowCloseButtons = true;
```

When split mode is active:
- **Existing content preserved**: All panels remain visible and functional
- **Right-click splitting**: Right-click on any panel to split it
- **Keyboard shortcuts**: Use keyboard shortcuts for quick splitting
- **Visual feedback**: Active split areas show visual indicators
- **Toggle off safely**: Disabling split mode preserves all existing panels

### Automatic Content Wrapping

The library automatically handles different content types:

```csharp
private void OnContentRequested(object sender, ContentRequestedEventArgs e)
{
    // Return any UIElement - FlexiPanel automatically wraps it for splitting
    e.RequestedContent = new UserControl(); // Gets wrapped in FlexiPaneItem
    e.RequestedContent = new TextBlock();   // Gets wrapped in FlexiPaneItem
    e.RequestedContent = new FlexiPaneItem(); // Used directly
}
```

### Smart Layout Management

The panel intelligently manages complex layouts:

- **Automatic Structure Preservation**: When split mode is toggled off, existing split layouts are preserved
- **Recursive Split Control**: All nested panels automatically inherit split mode settings
- **Dynamic Content Loading**: Content is created on-demand when panels are split
- **Memory Efficient**: Only active panels consume resources

### Selection and Focus

The library automatically tracks the selected panel:

```csharp
// Get the currently selected panel
var selected = flexiPanel.SelectedItem;

// Set selection programmatically
somePane.IsSelected = true;

// Track selection changes
flexiPanel.SelectionChanged += (s, e) => {
    Console.WriteLine($"Selected: {e.NewSelection?.Title}");
};
```

### Minimal Configuration Usage

For the absolute simplest setup:

```xml
<!-- This works out of the box! -->
<flexiPane:FlexiPanel />
```

The panel will:
- Display helpful instruction text initially
- Activate split functionality when `IsSplitModeActive` is set to `true`
- Generate default content automatically if no event handlers are provided
- Handle all split operations seamlessly

## 🛠️ Requirements

- **.NET 9.0** or later
- **Windows** platform (WPF dependency)
- **WPF** application project

## 📚 Documentation

- [Architecture Design](docs/architecture.md) - System architecture and design principles
- [Splitting Mechanism](docs/splitting-mechanism.md) - Detailed splitting algorithms and tree management

## 🎮 Demo Application

Check out the [FlexiPane.Samples.DefaultApp](src/FlexiPane.Samples.DefaultApp) project for complete working examples:

### Simple Demo (MainWindowSimple.xaml)
- ✨ **Minimal Setup**: Just a ToggleButton and FlexiPanel
- 🎮 **Event-Driven Content**: Uses ContentRequested and PaneSplitRequested events
- 🎨 **Random Colored Panels**: Each panel gets a unique color and timestamp
- 🛡️ **Safe Mode Toggling**: Toggle split mode on/off without losing panels
- 🔄 **Automatic Content Wrapping**: Returns simple Border elements that get auto-wrapped

### Full-Featured Demo (MainWindow.xaml)  
- 🎨 Multiple content types (editor, terminal, explorer)
- 💾 Layout save/load functionality
- 🎛️ Content factory system usage
- ⌨️ Keyboard shortcuts and navigation
- 🎯 Programmatic control APIs

To run the demo:
```bash
git clone https://github.com/iyulab/FlexiPane.WPF.git
cd FlexiPane.WPF
dotnet run --project src/FlexiPane.Samples.DefaultApp/
```

The simple demo shows the **minimum code required** to get a fully functional split panel system working!

## 🤝 Contributing

Contributions are welcome! Please feel free to:

1. 🐛 **Report bugs** by opening an issue
2. 💡 **Suggest features** for future versions
3. 🔧 **Submit pull requests** with improvements
4. 📖 **Improve documentation** or add examples
5. ⭐ **Star the repository** if you find it useful!

Please read our [contributing guidelines](CONTRIBUTING.md) before submitting PRs.

## 📄 License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

## ⭐ Credits

**Developed by [iyulab](https://github.com/iyulab)**

Built with modern WPF best practices and inspired by proven splitting mechanisms from advanced development environments.

## 🚀 Release Status

✅ **Production Ready** - The library is stable and ready for production use. Semantic versioning is followed for all releases.

### Current Version: v1.0.0
- ✅ **Simple Integration**: Works with minimal XAML configuration
- ✅ **Event-Driven Architecture**: ContentRequested and PaneSplitRequested events  
- ✅ **Smart Split Mode**: Toggle on/off while preserving existing layouts
- ✅ **Automatic Content Wrapping**: Any UIElement becomes splittable
- ✅ **Layout Preservation**: Split mode toggling doesn't destroy content
- ✅ **Comprehensive Demo**: Both simple and full-featured examples
- ✅ **Zero Configuration**: Works out of the box with sensible defaults
- ✅ **Full XAML theming support**