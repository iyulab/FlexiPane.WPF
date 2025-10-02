using System;
using System.Windows;
using System.Windows.Controls;
using FlexiPane.Controls;
using FlexiPane.Events;

namespace FlexiPane.Managers;

/// <summary>
/// FlexiPane manager for split and close operations
/// Focus: Simple, predictable, synchronous operations with proper visual tree management
/// </summary>
public static class FlexiPaneManager
{
    /// <summary>
    /// Split FlexiPaneItem into FlexiPaneContainer with two children
    /// Follows splitting-mechanism.md patterns exactly
    /// </summary>
    public static FlexiPaneContainer? SplitPane(
        FlexiPaneItem sourcePane,
        bool isVerticalSplit,
        double splitRatio,
        UIElement? newContent = null)
    {
        // 1. Validate inputs
        if (sourcePane == null)
        {
            return null;
        }

        var flexiPanel = FlexiPanel.FindAncestorPanel(sourcePane);
        if (flexiPanel == null)
        {
            return null;
        }

        // 2. Create new container and items synchronously
        var newContainer = CreateSplitContainer(sourcePane, isVerticalSplit, splitRatio, newContent, flexiPanel);
        if (newContainer == null)
        {
            return null;
        }

        // 3. Replace sourcePane with newContainer in tree
        bool success = ReplaceInTree(sourcePane, newContainer, flexiPanel);
        if (!success)
        {
            return null;
        }

        return newContainer;
    }

    /// <summary>
    /// Create split container with forced template application
    /// </summary>
    private static FlexiPaneContainer? CreateSplitContainer(
        FlexiPaneItem sourcePane, 
        bool isVerticalSplit, 
        double splitRatio, 
        UIElement? newContent,
        FlexiPanel flexiPanel)
    {
        // Create container with properties set first
        var container = new FlexiPaneContainer
        {
            IsVerticalSplit = isVerticalSplit,
            SplitRatio = Math.Max(0.1, Math.Min(0.9, splitRatio))
        };

        // Force immediate template application BEFORE setting children
        
        // Multiple attempts to ensure template is applied
        for (int i = 0; i < 3; i++)
        {
            container.ApplyTemplate();
            container.Measure(new Size(1000, 1000));
            container.Arrange(new Rect(0, 0, 1000, 1000));
            container.UpdateLayout();
            
            // Force UI thread processing
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
                System.Windows.Threading.DispatcherPriority.Render, 
                new Action(() => { }));
        }

        // Create new pane
        var newPane = new FlexiPaneItem
        {
            Content = newContent ?? CreateDefaultContent(),
            CanSplit = flexiPanel.IsSplitModeActive
        };

        // Apply global split guide settings to the new pane
        ApplyGlobalSplitGuideSettings(newPane, flexiPanel);

        // Also apply to source pane in case it doesn't have the settings
        ApplyGlobalSplitGuideSettings(sourcePane, flexiPanel);

        // Now set children after template is ready
        
        container.FirstChild = sourcePane;
        container.SecondChild = newPane;

        // Final layout update
        container.UpdateLayout();

        return container;
    }

    /// <summary>
    /// Replace element in tree structure following splitting-mechanism.md patterns
    /// </summary>
    private static bool ReplaceInTree(UIElement oldElement, UIElement newElement, FlexiPanel flexiPanel)
    {
        // Case 1: sourcePane is FlexiPanel.RootContent (Scenario 1 from docs)
        if (flexiPanel.RootContent == oldElement)
        {
            flexiPanel.RootContent = newElement;
            return true;
        }

        // Case 2: sourcePane is child of FlexiPaneContainer (Scenario 2+ from docs)
        var parentContainer = FindParentContainer(oldElement, flexiPanel);
        if (parentContainer != null)
        {
            if (parentContainer.FirstChild == oldElement)
            {
                parentContainer.FirstChild = newElement;
                return true;
            }
            else if (parentContainer.SecondChild == oldElement)
            {
                parentContainer.SecondChild = newElement;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Find parent container of element in tree
    /// </summary>
    private static FlexiPaneContainer? FindParentContainer(UIElement element, FlexiPanel flexiPanel)
    {
        // Check RootContent first
        if (flexiPanel.RootContent is FlexiPaneContainer rootContainer)
        {
            if (rootContainer.FirstChild == element || rootContainer.SecondChild == element)
            {
                return rootContainer;
            }

            // Recursively search in children
            var result = FindParentContainerRecursive(element, rootContainer);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    /// <summary>
    /// Recursively find parent container
    /// </summary>
    private static FlexiPaneContainer? FindParentContainerRecursive(UIElement element, FlexiPaneContainer container)
    {
        // Check direct children
        if (container.FirstChild == element || container.SecondChild == element)
        {
            return container;
        }

        // Check nested containers
        if (container.FirstChild is FlexiPaneContainer firstContainer)
        {
            var result = FindParentContainerRecursive(element, firstContainer);
            if (result != null) return result;
        }

        if (container.SecondChild is FlexiPaneContainer secondContainer)
        {
            var result = FindParentContainerRecursive(element, secondContainer);
            if (result != null) return result;
        }

        return null;
    }

    /// <summary>
    /// Create default content for new panes
    /// </summary>
    private static UIElement CreateDefaultContent()
    {
        var border = new Border
        {
            Background = System.Windows.Media.Brushes.LightBlue,
            Padding = new Thickness(10)
        };

        var textBlock = new TextBlock
        {
            Text = $"New Panel\n{DateTime.Now:HH:mm:ss}",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        border.Child = textBlock;
        return border;
    }

    /// <summary>
    /// Close pane following removal algorithm from splitting-mechanism.md
    /// </summary>
    public static bool ClosePane(FlexiPaneItem paneToClose)
    {
        var flexiPanel = FlexiPanel.FindAncestorPanel(paneToClose);
        if (flexiPanel == null)
        {
            return false;
        }

        // Case 1: Last panel (RootContent is FlexiPaneItem)
        if (flexiPanel.RootContent == paneToClose)
        {
            flexiPanel.RootContent = null!;
            return true;
        }

        // Case 2: Panel in container
        var parentContainer = FindParentContainer(paneToClose, flexiPanel);
        if (parentContainer == null)
        {
            return false;
        }

        // Step 2: Determine sibling
        UIElement? sibling = null;
        if (parentContainer.FirstChild == paneToClose)
        {
            sibling = parentContainer.SecondChild;
        }
        else if (parentContainer.SecondChild == paneToClose)
        {
            sibling = parentContainer.FirstChild;
        }

        if (sibling == null)
        {
            return false;
        }

        // Step 3: Properly detach sibling and dispose container
        
        // First, detach sibling from container
        if (parentContainer.FirstChild == sibling)
        {
            parentContainer.FirstChild = null!;
        }
        else if (parentContainer.SecondChild == sibling)
        {
            parentContainer.SecondChild = null!;
        }

        // Also detach the pane being closed to ensure clean disposal
        if (parentContainer.FirstChild == paneToClose)
        {
            parentContainer.FirstChild = null!;
        }
        else if (parentContainer.SecondChild == paneToClose)
        {
            parentContainer.SecondChild = null!;
        }

        // Dispose of the container to clean up its template and prevent visual tree conflicts
        if (parentContainer is IDisposable disposableContainer)
        {
            disposableContainer.Dispose();
        }

        // Step 4: Replace container with sibling
        bool success = ReplaceInTree(parentContainer, sibling, flexiPanel);

        return success;
    }

    /// <summary>
    /// Connect pane events
    /// </summary>
    public static void ConnectPaneEvents(FlexiPaneItem pane)
    {
        // Simplified event connection
        if (pane != null)
        {
            pane.Closed += OnPaneClosed;
        }
    }

    /// <summary>
    /// Disconnect pane events
    /// </summary>
    public static void DisconnectPaneEvents(FlexiPaneItem pane)
    {
        if (pane != null)
        {
            pane.Closed -= OnPaneClosed;
        }
    }

    /// <summary>
    /// Handle pane closed event
    /// </summary>
    private static void OnPaneClosed(object? sender, PaneClosedEventArgs e)
    {
        if (e.Pane != null)
        {
            DisconnectPaneEvents(e.Pane);
        }
    }

    /// <summary>
    /// Apply global split guide settings from FlexiPanel to a FlexiPaneItem
    /// </summary>
    private static void ApplyGlobalSplitGuideSettings(FlexiPaneItem paneItem, FlexiPanel flexiPanel)
    {
        if (paneItem == null || flexiPanel == null) return;

        // Apply global split guide content if the item doesn't have its own
        if (paneItem.SplitGuideContent == null && flexiPanel.SplitGuideContent != null)
        {
            paneItem.SplitGuideContent = flexiPanel.SplitGuideContent;
        }

        // Apply global split guide content template if the item doesn't have its own
        if (paneItem.SplitGuideContentTemplate == null && flexiPanel.SplitGuideContentTemplate != null)
        {
            paneItem.SplitGuideContentTemplate = flexiPanel.SplitGuideContentTemplate;
        }
    }
}