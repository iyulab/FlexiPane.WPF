# FlexiPane.WPF Architecture Design

## Overview

FlexiPane.WPF is a versatile library that provides dynamic screen splitting functionality for WPF applications. Designed based on proven splitting mechanisms from the FastFinder project, it offers high compatibility and performance with pure WPF implementation.

## Core Components

### FlexiPanel
The root container that hosts and manages the entire pane structure.

**Key Features:**
- Root content management
- Split mode activation control
- Event routing and bubbling
- Selected item tracking
- Programmatic split methods

### FlexiPaneItem
A splittable panel that contains actual user content.

**Key Features:**
- User content hosting
- Unique identifier management
- Title/header display
- Close functionality control
- Split capability settings
- Selection state management
- Focus handling
- Event-based interaction

### FlexiPaneContainer
A split container that holds two children with GridSplitter.

**Key Features:**
- First child (left or top) management
- Second child (right or bottom) management
- Split direction control (vertical/horizontal)
- Split ratio adjustment (0.1 ~ 0.9)
- Real-time resize via GridSplitter
- Automatic layout management

### FlexiPaneManager
Static manager class that handles splitting and removal logic.

**Key Features:**
- Panel split processing
- Panel removal management
- Structure simplification
- Tree structure reorganization
- Parent-child relationship management

## Event System

### Core Events

**PaneSplitRequestedEventArgs**
- Event data raised when split is requested
- Contains source panel, split direction, ratio information
- Cancelable event with custom content support

**PaneClosingEventArgs**
- Event data raised when panel close is requested
- Contains target panel and close reason information
- Cancelable event for validation

**PaneClosedEventArgs**
- Event data raised after panel is closed
- Non-cancelable completion event

**LastPaneClosingEventArgs**
- Event data raised when closing the last remaining panel
- Cancelable with default behavior configuration

**NewPaneCreatedEventArgs**
- Event data raised when new panel is created
- Contains new panel and source panel references

## Visual System

### Themes and Styling
The library uses WPF's theme system for visual customization.

**Theme Elements:**
- Default control templates in Generic.xaml
- Selection border visualization
- Split mode overlay with guide content
- GridSplitter styling
- Close button appearance
- Visual state management

### Split Mode UI
Interactive split mode with visual feedback:
- Semi-transparent overlay
- Guide lines showing split preview
- Custom guide content support
- Responsive mouse tracking
- Edge detection for split direction

## Design Principles

### 1. Pure WPF Implementation
- No external dependencies, pure WPF implementation
- .NET 9.0 based with backward compatibility considerations
- Hardware-accelerated rendering utilization

### 2. MVVM Friendly
- Data binding first API design
- Command pattern support
- Dependency property based implementation
- INotifyPropertyChanged integration

### 3. Extensibility
- Event-based extension points
- Custom content support
- Template-based customization
- Pluggable behavior system

### 4. Performance Optimization
- Smart resource management
- IDisposable pattern implementation
- Optimized rendering paths
- Efficient tree traversal algorithms

### 5. Accessibility
- Keyboard navigation support (ESC key handling)
- Focus management
- Selection state visualization
- Screen reader compatibility ready

## Usage Scenarios

### Basic Usage
Simply place a FlexiPanel control in XAML with minimal configuration:
```xml
<flexiPane:FlexiPanel />
```

### Programmatic Splitting
Use built-in methods to split the selected pane:
```csharp
flexiPanel.SplitSelectedVertically(0.5, customContent);
flexiPanel.SplitSelectedHorizontally(0.5, customContent);
```

### Event-Driven Customization
Handle events to customize split behavior and content:
- PaneSplitRequested - Provide custom content for new panes
- PaneClosing - Validate close operations
- LastPaneClosing - Handle last panel scenarios
- NewPaneCreated - Initialize new panels

## Architecture Patterns

### Tree Structure Management
- Binary tree structure with FlexiPaneContainer nodes
- Leaf nodes are FlexiPaneItem instances
- Automatic structure simplification when nodes are removed
- Parent reference tracking for efficient traversal

### Event Routing Strategy
- Routed events bubble from FlexiPaneItem to FlexiPanel
- Dispatcher-based delayed processing for custom handlers
- Centralized event handling in FlexiPanel
- Manager delegation for actual operations

### Resource Lifecycle
- Proper cleanup in Dispose methods
- Event handler disconnection on unload
- Visual tree aware operations
- Memory leak prevention through weak references

## Differentiation Factors

### 1. Simplicity
- Minimal configuration required
- Sensible defaults for immediate use
- Clean and intuitive API

### 2. Flexibility
- Full XAML template customization
- Event-driven architecture
- Multiple extension points

### 3. Robustness
- Proven splitting algorithms
- Memory leak prevention
- Exception safety guarantees
- Comprehensive null safety

### 4. Modern Design
- .NET 9.0 latest features
- Nullable reference types
- Modern C# patterns
- Clean code principles

This architecture provides a best-in-class screen splitting solution in the WPF ecosystem, with a structure that developers can easily adopt and extend.