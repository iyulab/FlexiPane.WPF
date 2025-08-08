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
- 🎨 **Visual Split Mode** - Interactive split overlay with guide lines and custom content
- 🔄 **Smart Panel Management** - Automatic structure simplification when panels are removed
- 🎮 **Programmatic Control** - Built-in methods for splitting selected panels
- 🎯 **Focus & Selection** - Automatic focus tracking and visual selection indicators
- 📐 **Flexible Layouts** - Support for complex nested split arrangements
- 💾 **Layout Persistence** - Save and load complete layouts to XML files
- 🎨 **Fully Customizable** - XAML templates for complete visual customization
- ⚡ **High Performance** - Pure WPF implementation with optimized rendering
- 🔌 **Event-Driven Architecture** - Rich event system for custom behaviors
- 🎛️ **Content Factory System** - Pluggable content creation for different panel types
- ⌨️ **Full Keyboard Support** - Complete keyboard navigation and shortcuts
- 💾 **Zero Configuration** - Works out of the box with sensible defaults

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

Use the simplest possible configuration:

```xml
<flexiPane:FlexiPanel />
```

That's it! The panel will automatically initialize with default content and be ready for splitting.

### Programmatic Splitting

Split the selected panel programmatically:

```csharp
// Split vertically at 50%
flexiPanel.SplitSelectedVertically(0.5);

// Split horizontally with custom content
var customContent = new TextBlock { Text = "New Panel" };
flexiPanel.SplitSelectedHorizontally(0.3, customContent);
```

### Handling Events

Customize behavior through events:

```csharp
// Provide custom content for new panels
flexiPanel.PaneSplitRequested += (s, e) => {
    e.NewContent = CreateCustomContent();
};

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

Enable visual split mode for interactive splitting:

```csharp
flexiPanel.IsSplitModeActive = true;
flexiPanel.ShowCloseButtons = true;
```

Users can then:
- Click on panels to split them visually
- See guide lines showing where the split will occur
- Close panels with the X button
- Press ESC to exit split mode

### Layout Persistence

Save and load complete layouts:

```csharp
// Save current layout to file
flexiPanel.SaveLayoutToFile("my-layout.flexilayout");

// Load layout from file
flexiPanel.LoadLayoutFromFile("my-layout.flexilayout");

// Clear all panels
flexiPanel.Clear();
```

### Custom Split Guide Content

Provide custom content for the split overlay:

```xml
<flexiPane:FlexiPaneItem>
    <flexiPane:FlexiPaneItem.SplitGuideContent>
        <TextBlock Text="Click to split this panel" />
    </flexiPane:FlexiPaneItem.SplitGuideContent>
    <!-- Your content here -->
</flexiPane:FlexiPaneItem>
```

### Content Factory System

Register content creators for different panel types:

```csharp
// Register content creators
flexiPanel.RegisterContentCreator("editor", (paneInfo) => new TextEditor());
flexiPanel.RegisterContentCreator("browser", (paneInfo) => new WebBrowser());

// Set default content creator
flexiPanel.SetDefaultContentCreator((paneInfo) => new DefaultPanel());
```

### Selection and Focus

The library automatically tracks the selected panel:

```csharp
// Get the currently selected panel
var selected = flexiPanel.SelectedItem;

// Set selection programmatically
somePane.IsSelected = true;
```

## 🛠️ Requirements

- **.NET 9.0** or later
- **Windows** platform (WPF dependency)
- **WPF** application project

## 📚 Documentation

- [Architecture Design](docs/architecture.md) - System architecture and design principles
- [Splitting Mechanism](docs/splitting-mechanism.md) - Detailed splitting algorithms and tree management

## 🎮 Demo Application

Check out the [FlexiPane.Samples.DefaultApp](src/FlexiPane.Samples.DefaultApp) project for a complete working example demonstrating:
- ✨ Interactive panel splitting with visual feedback
- 🎮 Event handling and custom behaviors
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
- ✅ Complete core functionality
- ✅ Comprehensive event system  
- ✅ Layout persistence
- ✅ Full XAML theming support
- ✅ Demo application with examples
- ✅ NuGet package available