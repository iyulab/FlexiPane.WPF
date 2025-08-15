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
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[FlexiPaneManager] === SPLIT START ===");
        System.Diagnostics.Debug.WriteLine($"[FlexiPaneManager] Split {(isVerticalSplit ? "VERTICAL" : "HORIZONTAL")} at ratio {splitRatio:F2}");
#endif

        // 1. Validate inputs
        if (sourcePane == null)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[FlexiPaneManager] ERROR: sourcePane is null");
#endif
            return null;
        }

        var flexiPanel = FlexiPanel.FindAncestorPanel(sourcePane);
        if (flexiPanel == null)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[FlexiPaneManager] ERROR: No FlexiPanel found");
#endif
            return null;
        }

        // 2. Create new container and items synchronously
        var newContainer = CreateSplitContainer(sourcePane, isVerticalSplit, splitRatio, newContent, flexiPanel);
        if (newContainer == null)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[FlexiPaneManager] ERROR: Failed to create split container");
#endif
            return null;
        }

        // 3. Replace sourcePane with newContainer in tree
        bool success = ReplaceInTree(sourcePane, newContainer, flexiPanel);
        if (!success)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[FlexiPaneManager] ERROR: Failed to replace in tree");
#endif
            return null;
        }

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[FlexiPaneManager] === SPLIT SUCCESS ===");
#endif
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
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[FlexiPaneManager] Creating split container");
#endif

        // Create container with properties set first
        var container = new FlexiPaneContainer
        {
            IsVerticalSplit = isVerticalSplit,
            SplitRatio = Math.Max(0.1, Math.Min(0.9, splitRatio))
        };

        // Force immediate template application BEFORE setting children
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[FlexiPaneManager] Forcing template application BEFORE setting children");
#endif
        
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
            
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[FlexiPaneManager] Template application attempt {i + 1}");
#endif
        }

        // Create new pane
        var newPane = new FlexiPaneItem
        {
            Content = newContent ?? CreateDefaultContent(),
            CanSplit = flexiPanel.IsSplitModeActive
        };

        // Now set children after template is ready
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[FlexiPaneManager] Setting children AFTER template application");
#endif
        
        container.FirstChild = sourcePane;
        container.SecondChild = newPane;

        // Final layout update
        container.UpdateLayout();

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[FlexiPaneManager] Container created - FirstChild: {container.FirstChild?.GetType().Name}, SecondChild: {container.SecondChild?.GetType().Name}");
#endif

        return container;
    }

    /// <summary>
    /// Replace element in tree structure following splitting-mechanism.md patterns
    /// </summary>
    private static bool ReplaceInTree(UIElement oldElement, UIElement newElement, FlexiPanel flexiPanel)
    {
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[FlexiPaneManager] Replacing {oldElement.GetType().Name} with {newElement.GetType().Name}");
#endif

        // Case 1: sourcePane is FlexiPanel.RootContent (Scenario 1 from docs)
        if (flexiPanel.RootContent == oldElement)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[FlexiPaneManager] Replacing RootContent directly");
#endif
            flexiPanel.RootContent = newElement;
            return true;
        }

        // Case 2: sourcePane is child of FlexiPaneContainer (Scenario 2+ from docs)
        var parentContainer = FindParentContainer(oldElement, flexiPanel);
        if (parentContainer != null)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[FlexiPaneManager] Replacing in parent container");
#endif
            
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

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[FlexiPaneManager] ERROR: Could not find replacement location");
#endif
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
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[FlexiPaneManager] === CLOSE START ===");
#endif

        var flexiPanel = FlexiPanel.FindAncestorPanel(paneToClose);
        if (flexiPanel == null)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[FlexiPaneManager] ERROR: No FlexiPanel found");
#endif
            return false;
        }

        // Case 1: Last panel (RootContent is FlexiPaneItem)
        if (flexiPanel.RootContent == paneToClose)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[FlexiPaneManager] Closing last panel");
#endif
            flexiPanel.RootContent = null!;
            return true;
        }

        // Case 2: Panel in container
        var parentContainer = FindParentContainer(paneToClose, flexiPanel);
        if (parentContainer == null)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[FlexiPaneManager] ERROR: No parent container found");
#endif
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
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[FlexiPaneManager] ERROR: No sibling found");
#endif
            return false;
        }

        // Step 3: Properly detach sibling and dispose container
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[FlexiPaneManager] Detaching sibling from parent container");
#endif
        
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
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[FlexiPaneManager] Disposing parent container to prevent template conflicts");
#endif
        if (parentContainer is IDisposable disposableContainer)
        {
            disposableContainer.Dispose();
        }

        // Step 4: Replace container with sibling
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[FlexiPaneManager] Replacing FlexiPaneContainer with {sibling.GetType().Name}");
#endif
        bool success = ReplaceInTree(parentContainer, sibling, flexiPanel);

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[FlexiPaneManager] === CLOSE {(success ? "SUCCESS" : "FAILED")} ===");
#endif
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
}