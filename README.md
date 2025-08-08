# FlexiPane.WPF

A modern and flexible WPF screen splitting library for dynamic pane management. Create resizable, dockable panels with intuitive split controls and visual feedback.

## Features

- 🎯 **Dynamic Screen Splitting** - Split panels vertically or horizontally at runtime
- 🎨 **Visual Split Mode** - Interactive split overlay with guide lines and custom content
- 🔄 **Smart Panel Management** - Automatic structure simplification when panels are removed
- 🎮 **Programmatic Control** - Built-in methods for splitting selected panels
- 🎯 **Focus & Selection** - Automatic focus tracking and visual selection indicators
- 📐 **Flexible Layouts** - Support for complex nested split arrangements
- 🎨 **Fully Customizable** - XAML templates for complete visual customization
- ⚡ **High Performance** - Pure WPF implementation with optimized rendering
- 🔌 **Event-Driven Architecture** - Rich event system for custom behaviors
- 💾 **Zero Configuration** - Works out of the box with sensible defaults

## Quick Start

### Installation

```xml
<!-- Coming soon to NuGet -->
```

### Basic Usage

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

### Selection and Focus

The library automatically tracks the selected panel:

```csharp
// Get the currently selected panel
var selected = flexiPanel.SelectedItem;

// Set selection programmatically
somePane.IsSelected = true;
```

## Requirements

- .NET 9.0 or later
- Windows platform
- WPF application

## Documentation

- [Architecture Design](docs/architecture.md) - System architecture and design principles
- [Splitting Mechanism](docs/splitting-mechanism.md) - Detailed splitting algorithms and tree management
- [API Design](docs/api-design.md) - Complete API reference (coming soon)
- [Implementation Guide](docs/implementation-guide.md) - Step-by-step implementation details (coming soon)

## Samples

Check out the [FlexiPane.Samples.DefaultApp](src/FlexiPane.Samples.DefaultApp) project for a complete working example demonstrating:
- Basic panel splitting
- Event handling
- Custom content provision
- Split mode toggling
- Programmatic control

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Credits

Developed by [iyulab](https://github.com/iyulab)

Based on proven splitting mechanisms from the FastFinder project, reimagined as a standalone WPF library.

## Status

🚧 **Early Development** - The library is functional but still under active development. API may change in future versions.