using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FlexiPane.Controls;
using FlexiPane.Events;

namespace FlexiPane.Managers
{
    /// <summary>
    /// Static class responsible for FlexiPane splitting and management logic
    /// </summary>
    public static class FlexiPaneManager
    {
        /// <summary>
        /// Split FlexiPaneItem to convert it into a new FlexiPaneContainer
        /// </summary>
        /// <param name="sourcePane">Original panel to be split</param>
        /// <param name="isVerticalSplit">true: vertical split (left/right), false: horizontal split (top/bottom)</param>
        /// <param name="splitRatio">Split ratio (0.1 ~ 0.9)</param>
        /// <param name="newContent">Content for the new panel</param>
        /// <returns>Created FlexiPaneContainer or null on failure</returns>
        public static FlexiPaneContainer? SplitPane(
            FlexiPaneItem sourcePane,
            bool isVerticalSplit,
            double splitRatio,
            UIElement? newContent = null)
        {
#if DEBUG
            Debug.WriteLine($"[FlexiPaneManager] SplitPane START");
            Debug.WriteLine($"[FlexiPaneManager] Parameters - IsVertical: {isVerticalSplit}, Ratio: {splitRatio:F2}");
            Debug.WriteLine($"[FlexiPaneManager] SourcePane: {sourcePane?.GetType().Name ?? "null"}");
#endif

            if (sourcePane == null)
            {
#if DEBUG
                Debug.WriteLine($"[FlexiPaneManager] ERROR: sourcePane is null");
#endif
                return null;
            }

#if DEBUG
            Debug.WriteLine($"[FlexiPaneManager] SourcePane validation passed");
#endif

            // Validate split ratio
#if DEBUG
            Debug.WriteLine($"[FlexiPaneManager] Validating split ratio: {splitRatio:F2}");
#endif
            if (splitRatio < 0.1 || splitRatio > 0.9)
            {
#if DEBUG
                Debug.WriteLine($"[FlexiPaneManager] Invalid split ratio {splitRatio:F2}, using default 0.5");
#endif
                splitRatio = 0.5;
            }

#if DEBUG
            Debug.WriteLine($"[FlexiPaneManager] Split ratio validated: {splitRatio:F2}");
#endif

            // Find direct parent of sourcePane
#if DEBUG
            Debug.WriteLine($"[FlexiPaneManager] Finding sourcePane's direct parent");
#endif
            
            var directParent = FindDirectParent(sourcePane);
            if (directParent == null)
            {
#if DEBUG
                Debug.WriteLine($"[FlexiPaneManager] SplitPane failed - no direct parent found");
#endif
                return null;
            }

#if DEBUG
            Debug.WriteLine($"[FlexiPaneManager] Found direct parent: {directParent.GetType().Name}");
#endif

            try
            {
                // 1. Create new FlexiPaneContainer
                var container = new FlexiPaneContainer
                {
                    IsVerticalSplit = isVerticalSplit,
                    SplitRatio = splitRatio
                };

                // 2. Copy properties from source panel
                CopyCommonProperties(sourcePane, container);

                // 3. Create new panel
                var newPane = new FlexiPaneItem();
                if (newContent != null)
                {
                    newPane.Content = newContent;
                }
                else
                {
                    // Create default content
                    newPane.Content = CreateDefaultPaneContent();
                }

#if DEBUG
                Debug.WriteLine($"[FlexiPaneManager] Setting container children - First: {sourcePane.GetType().Name}, Second: {newPane.GetType().Name}");
#endif
                
                // 4. Remove sourcePane from existing parent (prevent logical parent conflict)
#if DEBUG
                Debug.WriteLine($"[FlexiPaneManager] Removing sourcePane from current parent to avoid logical parent conflicts");
#endif
                RemoveFromLogicalParent(sourcePane);
                
                // 5. Set panels in container
                container.FirstChild = sourcePane;
                container.SecondChild = newPane;

                // 6. Replace sourcePane with container in direct parent
#if DEBUG
                Debug.WriteLine($"[FlexiPaneManager] Replacing sourcePane with container in direct parent");
#endif
                ReplaceChild(directParent, sourcePane, container);

#if DEBUG
                Debug.WriteLine($"[FlexiPaneManager] Split process completed - Container created with {container.FirstChild?.GetType().Name} and {container.SecondChild?.GetType().Name}");
#endif

                // 6. Connect events
                ConnectPaneEvents(sourcePane);
                ConnectPaneEvents(newPane);

                // 7. Raise new panel created event
                var flexiPanel = FlexiPanel.FindAncestorPanel(container);
                if (flexiPanel != null)
                {
                    var newPaneCreatedArgs = new NewPaneCreatedEventArgs(newPane, sourcePane)
                    {
                        RoutedEvent = FlexiPanel.NewPaneCreatedEvent
                    };
                    flexiPanel.RaiseEvent(newPaneCreatedArgs);
                }

                return container;
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine($"[FlexiPaneManager] Split failed with exception: {ex.Message}");
#endif
                System.Diagnostics.Debug.WriteLine($"FlexiPaneManager.SplitPane failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Remove panel from FlexiPaneContainer and simplify structure if needed
        /// Follows the removal algorithm from splitting-mechanism.md
        /// </summary>
        /// <param name="containerToClose">Container that the panel to close belongs to</param>
        /// <param name="paneToClose">Panel to close</param>
        /// <returns>Success status</returns>
        public static bool ClosePane(FlexiPaneContainer containerToClose, FlexiPaneItem paneToClose)
        {
#if DEBUG
            Debug.WriteLine($"[FlexiPaneManager] ClosePane START - Container: {containerToClose?.GetHashCode()}, Pane: {paneToClose?.GetHashCode()}");
#endif
            if (containerToClose == null || paneToClose == null)
            {
#if DEBUG
                Debug.WriteLine($"[FlexiPaneManager] ClosePane - null parameters");
#endif
                return false;
            }

            try
            {
                // 1. Determine remaining panel (sibling)
                UIElement? sibling = null;
                if (containerToClose.FirstChild == paneToClose)
                {
                    sibling = containerToClose.SecondChild;
#if DEBUG
                    Debug.WriteLine($"[FlexiPaneManager] Closing FirstChild, SecondChild will remain: {sibling?.GetType().Name}");
#endif
                }
                else if (containerToClose.SecondChild == paneToClose)
                {
                    sibling = containerToClose.FirstChild;
#if DEBUG
                    Debug.WriteLine($"[FlexiPaneManager] Closing SecondChild, FirstChild will remain: {sibling?.GetType().Name}");
#endif
                }

                if (sibling == null)
                {
#if DEBUG
                    Debug.WriteLine($"[FlexiPaneManager] ClosePane - no sibling found");
#endif
                    return false;
                }

                // 2. Find parent's parent (grandParent) - including FlexiPanel
                var grandParent = containerToClose.Parent;
                DependencyObject? actualGrandParent = grandParent; // Parent that will actually perform replacement
                
                // When Parent is null, it could be FlexiPanel's RootContent
                if (grandParent == null)
                {
#if DEBUG
                    Debug.WriteLine($"[FlexiPaneManager] No direct parent found, searching for FlexiPanel via RootContent");
#endif
                    
                    // Find if it's set as FlexiPanel's RootContent
                    var flexiPanel = FlexiPanel.FindAncestorPanel(containerToClose);
                    if (flexiPanel != null && flexiPanel.RootContent == containerToClose)
                    {
#if DEBUG
                        Debug.WriteLine($"[FlexiPaneManager] Found FlexiPanel as RootContent parent");
#endif
                        grandParent = flexiPanel;
                        actualGrandParent = flexiPanel;
                    }
                    else
                    {
#if DEBUG
                        Debug.WriteLine($"[FlexiPaneManager] ClosePane - no grandParent found for container");
#endif
                        return false;
                    }
                }
                // When grandParent is Grid, need to find actual FlexiPaneContainer
                else if (grandParent is Grid grid)
                {
#if DEBUG
                    Debug.WriteLine($"[FlexiPaneManager] GrandParent is Grid, finding actual FlexiPaneContainer");
#endif
                    // Grid's parent should be FlexiPaneContainer
                    var containerParent = LogicalTreeHelper.GetParent(grid) ?? VisualTreeHelper.GetParent(grid);
                    if (containerParent is FlexiPaneContainer parentContainer)
                    {
#if DEBUG
                        Debug.WriteLine($"[FlexiPaneManager] Found parent FlexiPaneContainer for Grid");
#endif
                        actualGrandParent = parentContainer;
                    }
                    else
                    {
                        actualGrandParent = grandParent; // Use Grid itself
                    }
                }
                else
                {
                    actualGrandParent = grandParent;
                }

#if DEBUG
                Debug.WriteLine($"[FlexiPaneManager] Container grandParent: {grandParent.GetType().Name}, Actual grandParent: {actualGrandParent?.GetType().Name}");
#endif

                // 3. First detach sibling from container
                if (containerToClose.FirstChild == sibling)
                {
                    containerToClose.FirstChild = null!;
                }
                else if (containerToClose.SecondChild == sibling)
                {
                    containerToClose.SecondChild = null!;
                }

#if DEBUG
                Debug.WriteLine($"[FlexiPaneManager] Sibling disconnected from container");
#endif

                // 4. Simplify hierarchy: replace container with sibling in actualGrandParent
                ReplaceChild(actualGrandParent!, containerToClose, sibling);

                // 5. Clean up resources
                DisconnectPaneEvents(paneToClose);
                containerToClose.FirstChild = null!;
                containerToClose.SecondChild = null!;

                // 6. Check structure simplification - only perform when sibling is container
                // (When sibling is FlexiPaneItem, simplification is already done through replacement)
                if (sibling is FlexiPaneContainer)
                {
                    SimplifyStructure(actualGrandParent!);
                }

#if DEBUG
                Debug.WriteLine($"[FlexiPaneManager] ClosePane SUCCESS");
#endif
                return true;
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine($"[FlexiPaneManager] ClosePane EXCEPTION: {ex.Message}");
                Debug.WriteLine($"[FlexiPaneManager] Exception stack trace: {ex.StackTrace}");
#endif
                System.Diagnostics.Debug.WriteLine($"FlexiPaneManager.ClosePane failed: {ex.Message}");
                return false;
            }
        }

        #region Helper Methods

        /// <summary>
        /// Find direct parent of UIElement (FlexiPanel or FlexiPaneContainer)
        /// </summary>
        private static DependencyObject? FindDirectParent(UIElement element)
        {
            if (element == null) return null;

#if DEBUG
            Debug.WriteLine($"[FlexiPaneManager] FindDirectParent - Element: {element.GetType().Name}");
#endif

            // Check Logical Parent first
            var logicalParent = LogicalTreeHelper.GetParent(element);
            if (logicalParent != null)
            {
#if DEBUG
                Debug.WriteLine($"[FlexiPaneManager] Logical parent found: {logicalParent.GetType().Name}");
#endif
                
                // Go up until finding FlexiPanel or FlexiPaneContainer
                var current = logicalParent;
                while (current != null)
                {
                    if (current is FlexiPanel || current is FlexiPaneContainer)
                    {
#if DEBUG
                        Debug.WriteLine($"[FlexiPaneManager] Found target parent: {current.GetType().Name}");
#endif
                        return current;
                    }
                    
                    current = LogicalTreeHelper.GetParent(current) ?? VisualTreeHelper.GetParent(current);
                }
            }

            // Try Visual Parent as fallback
            var visualParent = VisualTreeHelper.GetParent(element);
            if (visualParent != null)
            {
#if DEBUG
                Debug.WriteLine($"[FlexiPaneManager] Visual parent found: {visualParent.GetType().Name}");
#endif
                
                var current = visualParent;
                while (current != null)
                {
                    if (current is FlexiPanel || current is FlexiPaneContainer)
                    {
#if DEBUG
                        Debug.WriteLine($"[FlexiPaneManager] Found target parent via visual tree: {current.GetType().Name}");
#endif
                        return current;
                    }
                    
                    current = VisualTreeHelper.GetParent(current);
                }
            }

#if DEBUG
            Debug.WriteLine($"[FlexiPaneManager] No suitable parent found");
#endif
            return null;
        }

        /// <summary>
        /// Find direct parent FlexiPaneContainer of FlexiPaneItem
        /// </summary>
        public static FlexiPaneContainer? FindDirectParentContainer(FlexiPaneItem pane)
        {
            if (pane == null) return null;

            // First check direct parent
            var directParent = pane.Parent;
#if DEBUG
            Debug.WriteLine($"[FlexiPaneManager] FindDirectParentContainer - Pane: {pane.GetHashCode()}, Direct parent: {directParent?.GetType().Name}");
#endif

            // When direct parent is Grid, check parent using Visual Tree
            if (directParent is Grid grid)
            {
                // Check both Logical Parent and Visual Parent
                var logicalParent = LogicalTreeHelper.GetParent(grid);
                var visualParent = VisualTreeHelper.GetParent(grid);
                
#if DEBUG
                Debug.WriteLine($"[FlexiPaneManager] Direct parent is Grid, checking parents - Logical: {logicalParent?.GetType().Name}, Visual: {visualParent?.GetType().Name}");
#endif
                
                // Check Logical Parent first
                if (logicalParent is FlexiPaneContainer logicalContainer)
                {
#if DEBUG
                    Debug.WriteLine($"[FlexiPaneManager] Found logical FlexiPaneContainer - FirstChild: {logicalContainer.FirstChild?.GetType().Name}, SecondChild: {logicalContainer.SecondChild?.GetType().Name}");
#endif
                    
                    // Check only if not an empty container
                    if (!IsEmptyContainer(logicalContainer) && 
                        (ContainsPaneRecursively(logicalContainer.FirstChild, pane) || ContainsPaneRecursively(logicalContainer.SecondChild, pane)))
                    {
#if DEBUG
                        Debug.WriteLine($"[FlexiPaneManager] Found pane within logical FlexiPaneContainer");
#endif
                        return logicalContainer;
                    }
#if DEBUG
                    else if (IsEmptyContainer(logicalContainer))
                    {
                        Debug.WriteLine($"[FlexiPaneManager] Skipping empty logical container");
                    }
#endif
                }
                
                // Check Visual Parent
                if (visualParent is FlexiPaneContainer visualContainer)
                {
#if DEBUG
                    Debug.WriteLine($"[FlexiPaneManager] Found visual FlexiPaneContainer - FirstChild: {visualContainer.FirstChild?.GetType().Name}, SecondChild: {visualContainer.SecondChild?.GetType().Name}");
#endif
                    
                    // Check only if not an empty container
                    if (!IsEmptyContainer(visualContainer) && 
                        (ContainsPaneRecursively(visualContainer.FirstChild, pane) || ContainsPaneRecursively(visualContainer.SecondChild, pane)))
                    {
#if DEBUG
                        Debug.WriteLine($"[FlexiPaneManager] Found pane within visual FlexiPaneContainer");
#endif
                        return visualContainer;
                    }
#if DEBUG
                    else if (IsEmptyContainer(visualContainer))
                    {
                        Debug.WriteLine($"[FlexiPaneManager] Skipping empty visual container");
                    }
#endif
                }
            }

            // Search both Visual Tree and Logical Tree
            var current = directParent;
            
            while (current != null)
            {
#if DEBUG
                Debug.WriteLine($"[FlexiPaneManager] Checking parent: {current.GetType().Name}");
#endif
                
                // When FlexiPaneContainer is found
                if (current is FlexiPaneContainer container)
                {
#if DEBUG
                    Debug.WriteLine($"[FlexiPaneManager] Found FlexiPaneContainer - FirstChild: {container.FirstChild?.GetType().Name ?? "null"}, SecondChild: {container.SecondChild?.GetType().Name ?? "null"}");
#endif
                    
                    // Recursively check if not empty container and if this container contains our pane
                    if (!IsEmptyContainer(container) && 
                        (ContainsPaneRecursively(container.FirstChild, pane) || ContainsPaneRecursively(container.SecondChild, pane)))
                    {
#if DEBUG
                        Debug.WriteLine($"[FlexiPaneManager] Found non-empty container that contains our pane");
#endif
                        return container;
                    }
#if DEBUG
                    else if (IsEmptyContainer(container))
                    {
                        Debug.WriteLine($"[FlexiPaneManager] Skipping empty container during tree traversal");
                    }
#endif
                }

                // Move to next parent
                if (current is FrameworkElement element)
                {
                    current = element.Parent;
                }
                else
                {
                    break;
                }
            }

#if DEBUG
            Debug.WriteLine($"[FlexiPaneManager] No direct parent container found");
#endif
            return null;
        }

        /// <summary>
        /// Recursively check if UIElement contains specific FlexiPaneItem
        /// </summary>
        private static bool ContainsPaneRecursively(UIElement? element, FlexiPaneItem targetPane)
        {
            if (element == null) return false;
            if (element == targetPane) return true;

            switch (element)
            {
                case FlexiPaneContainer container:
                    return ContainsPaneRecursively(container.FirstChild, targetPane) ||
                           ContainsPaneRecursively(container.SecondChild, targetPane);
                           
                case Panel panel:
                    foreach (UIElement child in panel.Children)
                    {
                        if (ContainsPaneRecursively(child, targetPane))
                            return true;
                    }
                    break;
                    
                case ContentControl contentControl:
                    if (contentControl.Content is UIElement contentElement)
                    {
                        return ContainsPaneRecursively(contentElement, targetPane);
                    }
                    break;
                    
                case Border border:
                    if (border.Child != null)
                    {
                        return ContainsPaneRecursively(border.Child, targetPane);
                    }
                    break;
            }

            return false;
        }

        /// <summary>
        /// Create default panel content
        /// </summary>
        private static UIElement CreateDefaultPaneContent()
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(240, 248, 255)), // Alice Blue
                BorderBrush = new SolidColorBrush(Color.FromRgb(135, 206, 235)), // Sky Blue
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(16)
            };

            var textBlock = new TextBlock
            {
                Text = $"New Split Panel\nCreated: {DateTime.Now:HH:mm:ss}",
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };

            border.Child = textBlock;
            return border;
        }

        /// <summary>
        /// Copy common properties
        /// </summary>
        private static void CopyCommonProperties(UIElement source, UIElement target)
        {
            if (source is not FrameworkElement sourceElement || target is not FrameworkElement targetElement)
                return;
                
            CopyFrameworkElementProperties(sourceElement, targetElement);
        }
        
        private static void CopyFrameworkElementProperties(FrameworkElement source, FrameworkElement target)
        {
            target.Width = source.Width;
            target.Height = source.Height;
            target.MinWidth = source.MinWidth;
            target.MinHeight = source.MinHeight;
            target.MaxWidth = source.MaxWidth;
            target.MaxHeight = source.MaxHeight;
            target.Margin = source.Margin;
            target.HorizontalAlignment = source.HorizontalAlignment;
            target.VerticalAlignment = source.VerticalAlignment;
        }

        /// <summary>
        /// Remove element from parent
        /// </summary>
        private static void RemoveFromParent(UIElement child, DependencyObject parent)
        {
#if DEBUG
            Debug.WriteLine($"[FlexiPaneManager] RemoveFromParent - Child: {child?.GetType().Name}, Parent: {parent?.GetType().Name}");
#endif
            switch (parent)
            {
                case Panel panel:
#if DEBUG
                    Debug.WriteLine($"[FlexiPaneManager] Removing from Panel - Panel type: {panel.GetType().Name}, Before count: {panel.Children.Count}");
#endif
                    panel.Children.Remove(child);
                    
                    // Reset Grid attached properties when removing from Grid
                    if (panel is Grid)
                    {
                        Grid.SetRow(child, 0);
                        Grid.SetColumn(child, 0);
                        Grid.SetRowSpan(child, 1);
                        Grid.SetColumnSpan(child, 1);
#if DEBUG
                        Debug.WriteLine($"[FlexiPaneManager] Reset Grid attached properties for removed child");
#endif
                    }
#if DEBUG
                    Debug.WriteLine($"[FlexiPaneManager] Removed from Panel - After count: {panel.Children.Count}");
#endif
                    break;
                case Border border:
                    if (border.Child == child)
                        border.Child = null;
                    break;
                case ContentControl contentControl:
                    if (contentControl.Content == child)
                        contentControl.Content = null;
                    break;
                case FlexiPaneContainer container:
                    if (container.FirstChild == child)
                        container.FirstChild = null!;
                    else if (container.SecondChild == child)
                        container.SecondChild = null!;
                    break;
                case FlexiPanel panel:
                    if (panel.RootContent == child)
                        panel.RootContent = null!;
                    break;
            }
        }

        /// <summary>
        /// Replace child element in parent (preserve position info)
        /// </summary>
        private static void ReplaceChild(DependencyObject parent, UIElement oldChild, UIElement newChild)
        {
#if DEBUG
            Debug.WriteLine($"[FlexiPaneManager] ReplaceChild - Parent: {parent?.GetType().Name}, Old: {oldChild?.GetType().Name}, New: {newChild?.GetType().Name}");
#endif
            switch (parent)
            {
                case Panel panel:
#if DEBUG
                    Debug.WriteLine($"[FlexiPaneManager] Replacing in Panel - Panel type: {panel.GetType().Name}");
#endif
                    var index = panel.Children.IndexOf(oldChild);
                    if (index >= 0)
                    {
                        // Backup Grid attached properties
                        if (panel is Grid)
                        {
                            var row = Grid.GetRow(oldChild);
                            var column = Grid.GetColumn(oldChild);
                            var rowSpan = Grid.GetRowSpan(oldChild);
                            var columnSpan = Grid.GetColumnSpan(oldChild);

#if DEBUG
                            Debug.WriteLine($"[FlexiPaneManager] Preserving Grid position - Row: {row}, Column: {column}, RowSpan: {rowSpan}, ColumnSpan: {columnSpan}");
#endif
                            
                            panel.Children.RemoveAt(index);
                            panel.Children.Insert(index, newChild);
                            
                            // Apply position info to new element
                            Grid.SetRow(newChild, row);
                            Grid.SetColumn(newChild, column);
                            Grid.SetRowSpan(newChild, rowSpan);
                            Grid.SetColumnSpan(newChild, columnSpan);
                        }
                        else
                        {
                            panel.Children.RemoveAt(index);
                            panel.Children.Insert(index, newChild);
                        }
#if DEBUG
                        Debug.WriteLine($"[FlexiPaneManager] Replaced in Panel - Index: {index}");
#endif
                    }
                    break;

                case Border border:
#if DEBUG
                    Debug.WriteLine($"[FlexiPaneManager] Replacing Border.Child");
#endif
                    if (border.Child == oldChild)
                        border.Child = newChild;
                    break;
                    
                case ContentControl contentControl:
#if DEBUG
                    Debug.WriteLine($"[FlexiPaneManager] Replacing ContentControl.Content");
#endif
                    if (contentControl.Content == oldChild)
                        contentControl.Content = newChild;
                    break;
                    
                case FlexiPaneContainer container:
#if DEBUG
                    Debug.WriteLine($"[FlexiPaneManager] Replacing in FlexiPaneContainer");
#endif
                    if (container.FirstChild == oldChild)
                        container.FirstChild = (newChild as UIElement) ?? newChild;
                    else if (container.SecondChild == oldChild)
                        container.SecondChild = (newChild as UIElement) ?? newChild;
                    break;
                    
                case FlexiPanel panel:
#if DEBUG
                    Debug.WriteLine($"[FlexiPaneManager] Replacing FlexiPanel.RootContent");
#endif
                    if (panel.RootContent == oldChild)
                        panel.RootContent = newChild;
                    break;
                    
                default:
#if DEBUG
                    Debug.WriteLine($"[FlexiPaneManager] Unknown parent type for replacement: {parent?.GetType().Name}");
#endif
                    break;
            }
#if DEBUG
            Debug.WriteLine($"[FlexiPaneManager] ReplaceChild COMPLETED");
#endif
        }

        /// <summary>
        /// Add element to parent
        /// </summary>
        private static void AddToParent(UIElement child, DependencyObject parent)
        {
#if DEBUG
            Debug.WriteLine($"[FlexiPaneManager] AddToParent START");
            Debug.WriteLine($"[FlexiPaneManager] Child type: {child?.GetType().Name ?? "null"}");
            Debug.WriteLine($"[FlexiPaneManager] Parent type: {parent?.GetType().Name ?? "null"}");
#endif
            switch (parent)
            {
                case Panel panel:
#if DEBUG
                    Debug.WriteLine($"[FlexiPaneManager] Adding to Panel - Panel type: {panel.GetType().Name}, Current children count: {panel.Children.Count}");
#endif
                    // Handle Grid to prevent overlapping child positions
                    if (panel is Grid grid)
                    {
                        // FlexiPaneContainer's Grid manages layout internally, so just add
                        grid.Children.Add(child);
#if DEBUG
                        Debug.WriteLine($"[FlexiPaneManager] Added to Grid without position - children count: {grid.Children.Count}");
#endif
                    }
                    else
                    {
                        panel.Children.Add(child);
#if DEBUG
                        Debug.WriteLine($"[FlexiPaneManager] Added to Panel - New children count: {panel.Children.Count}");
#endif
                    }
                    break;
                case Border border:
#if DEBUG
                    Debug.WriteLine($"[FlexiPaneManager] Setting Border.Child - Previous child: {border.Child?.GetType().Name ?? "null"}");
#endif
                    try
                    {
#if DEBUG
                        Debug.WriteLine($"[FlexiPaneManager] About to set Border.Child");
#endif
                        border.Child = child;
#if DEBUG
                        Debug.WriteLine($"[FlexiPaneManager] Successfully set Border.Child");
                        Debug.WriteLine($"[FlexiPaneManager] New child type: {border.Child?.GetType().Name ?? "null"}");
#endif
                    }
                    catch (Exception ex)
                    {
#if DEBUG
                        Debug.WriteLine($"[FlexiPaneManager] EXCEPTION setting Border.Child: {ex.Message}");
                        Debug.WriteLine($"[FlexiPaneManager] Exception stack trace: {ex.StackTrace}");
#endif
                        throw;
                    }
                    break;
                case ContentControl contentControl:
#if DEBUG
                    Debug.WriteLine($"[FlexiPaneManager] Setting ContentControl.Content - Previous content: {contentControl.Content?.GetType().Name ?? "null"}");
#endif
                    try
                    {
#if DEBUG
                        Debug.WriteLine($"[FlexiPaneManager] About to set ContentControl.Content");
#endif
                        contentControl.Content = child;
#if DEBUG
                        Debug.WriteLine($"[FlexiPaneManager] Successfully set ContentControl.Content");
                        Debug.WriteLine($"[FlexiPaneManager] New content type: {contentControl.Content?.GetType().Name ?? "null"}");
#endif
                    }
                    catch (Exception ex)
                    {
#if DEBUG
                        Debug.WriteLine($"[FlexiPaneManager] EXCEPTION setting ContentControl.Content: {ex.Message}");
                        Debug.WriteLine($"[FlexiPaneManager] Exception stack trace: {ex.StackTrace}");
#endif
                        throw;
                    }
                    break;
                case FlexiPaneContainer container:
#if DEBUG
                    Debug.WriteLine($"[FlexiPaneManager] Adding to FlexiPaneContainer");
#endif
                    // Add to first empty slot
                    if (container.FirstChild == null)
                        container.FirstChild = (child as UIElement) ?? child;
                    else if (container.SecondChild == null)
                        container.SecondChild = (child as UIElement) ?? child;
                    break;
                default:
#if DEBUG
                    Debug.WriteLine($"[FlexiPaneManager] Unknown parent type: {parent?.GetType().Name ?? "null"}");
#endif
                    break;
            }
#if DEBUG
            Debug.WriteLine($"[FlexiPaneManager] AddToParent COMPLETED");
#endif
        }

        /// <summary>
        /// Connect panel events
        /// </summary>
        public static void ConnectPaneEvents(FlexiPaneItem pane)
        {
            if (pane == null) return;

            // Only connect Closed event (split is handled through routed events)
            pane.Closed += OnPaneClosed;
        }

        /// <summary>
        /// Disconnect panel events
        /// </summary>
        public static void DisconnectPaneEvents(FlexiPaneItem pane)
        {
            if (pane == null) return;

            pane.Closed -= OnPaneClosed;
        }

        /// <summary>
        /// Remove UIElement from current logical parent
        /// </summary>
        private static void RemoveFromLogicalParent(UIElement element)
        {
            if (element == null) return;

#if DEBUG
            Debug.WriteLine($"[FlexiPaneManager] RemoveFromLogicalParent - Element: {element.GetType().Name}");
#endif

            // Find parent through Visual Tree and Logical Tree
            DependencyObject logicalParent = LogicalTreeHelper.GetParent(element);
            DependencyObject visualParent = VisualTreeHelper.GetParent(element);

#if DEBUG
            Debug.WriteLine($"[FlexiPaneManager] LogicalParent: {logicalParent?.GetType().Name ?? "null"}");
            Debug.WriteLine($"[FlexiPaneManager] VisualParent: {visualParent?.GetType().Name ?? "null"}");
#endif

            // Process Logical Parent first
            if (logicalParent != null)
            {
#if DEBUG
                Debug.WriteLine($"[FlexiPaneManager] Removing from logical parent: {logicalParent.GetType().Name}");
#endif
                RemoveFromParent(element, logicalParent);
            }
            // Process when Visual Parent is different
            else if (visualParent != null && visualParent != logicalParent)
            {
#if DEBUG
                Debug.WriteLine($"[FlexiPaneManager] Removing from visual parent: {visualParent.GetType().Name}");
#endif
                RemoveFromParent(element, visualParent);
            }

#if DEBUG
            Debug.WriteLine($"[FlexiPaneManager] RemoveFromLogicalParent completed");
#endif
        }


        /// <summary>
        /// Check if container is empty
        /// </summary>
        private static bool IsEmptyContainer(FlexiPaneContainer container)
        {
            return container != null && 
                   container.FirstChild == null && 
                   container.SecondChild == null;
        }
        
        /// <summary>
        /// Count total number of panels in container
        /// </summary>
        private static int CountPanesInContainer(UIElement? element)
        {
            if (element == null) return 0;
            
            switch (element)
            {
                case FlexiPaneItem:
                    return 1;
                    
                case FlexiPaneContainer container:
                    return CountPanesInContainer(container.FirstChild) + 
                           CountPanesInContainer(container.SecondChild);
                           
                default:
                    return 0;
            }
        }
        
        /// <summary>
        /// Structure simplification - follows rules defined in splitting-mechanism.md
        /// When container has only one child, promote that child to parent level
        /// </summary>
        private static void SimplifyStructure(DependencyObject parent)
        {
            if (parent == null) return;
            
#if DEBUG
            Debug.WriteLine($"[FlexiPaneManager] SimplifyStructure - Parent: {parent.GetType().Name}");
#endif
            
            // FlexiPanel case
            if (parent is FlexiPanel flexiPanel)
            {
                if (flexiPanel.RootContent is FlexiPaneContainer rootContainer)
                {
                    SimplifyContainer(rootContainer, flexiPanel, null);
                }
            }
            // FlexiPaneContainer case
            else if (parent is FlexiPaneContainer parentContainer)
            {
                // Simplify child containers of parent container
                if (parentContainer.FirstChild is FlexiPaneContainer firstContainer)
                {
                    SimplifyContainer(firstContainer, null, parentContainer);
                }
                if (parentContainer.SecondChild is FlexiPaneContainer secondContainer)
                {
                    SimplifyContainer(secondContainer, null, parentContainer);
                }
            }
            // Grid case (FlexiPaneContainer internal implementation)
            else if (parent is Grid)
            {
                // Find FlexiPanel and simplify entire structure
                var panel = FlexiPanel.FindAncestorPanel(parent);
                if (panel != null && panel.RootContent is FlexiPaneContainer rootContainer)
                {
#if DEBUG
                    Debug.WriteLine($"[FlexiPaneManager] Found FlexiPanel from Grid, simplifying RootContent");
#endif
                    SimplifyContainer(rootContainer, panel, null);
                }
            }
        }
        
        /// <summary>
        /// Simplify individual container - promote single child when only one exists
        /// </summary>
        private static void SimplifyContainer(FlexiPaneContainer container, FlexiPanel? flexiPanel, FlexiPaneContainer? parentContainer)
        {
            if (container == null) return;
            
#if DEBUG
            Debug.WriteLine($"[FlexiPaneManager] SimplifyContainer - FirstChild: {container.FirstChild?.GetType().Name ?? "null"}, SecondChild: {container.SecondChild?.GetType().Name ?? "null"}");
#endif
            
            // Remove empty container from parent
            if (IsEmptyContainer(container))
            {
#if DEBUG
                Debug.WriteLine($"[FlexiPaneManager] Found empty container, removing from parent");
#endif
                if (parentContainer != null)
                {
                    if (parentContainer.FirstChild == container)
                    {
                        parentContainer.FirstChild = null!;
                    }
                    else if (parentContainer.SecondChild == container)
                    {
                        parentContainer.SecondChild = null!;
                    }
                }
                return;
            }
            
            // First recursively simplify child containers
            if (container.FirstChild is FlexiPaneContainer firstChildContainer)
            {
                SimplifyContainer(firstChildContainer, null, container);
                // Check if became empty container after simplification
                if (IsEmptyContainer(firstChildContainer))
                {
                    container.FirstChild = null!;
                }
            }
            if (container.SecondChild is FlexiPaneContainer secondChildContainer)
            {
                SimplifyContainer(secondChildContainer, null, container);
                // Check if became empty container after simplification
                if (IsEmptyContainer(secondChildContainer))
                {
                    container.SecondChild = null!;
                }
            }
            
            // Check if container has only one child
            UIElement? singleChild = null;
            if (container.FirstChild != null && container.SecondChild == null)
            {
                singleChild = container.FirstChild;
            }
            else if (container.SecondChild != null && container.FirstChild == null)
            {
                singleChild = container.SecondChild;
            }
            
            // Promote when only one child exists
            if (singleChild != null)
            {
#if DEBUG
                Debug.WriteLine($"[FlexiPaneManager] Container has single child: {singleChild.GetType().Name}");
#endif
                
                // FlexiPanel's RootContent case
                if (flexiPanel != null && flexiPanel.RootContent == container)
                {
#if DEBUG
                    Debug.WriteLine($"[FlexiPaneManager] Promoting single child to RootContent for structure simplification");
#endif
                    // Detach child from container
                    container.FirstChild = null!;
                    container.SecondChild = null!;
                    // Promote single child to RootContent (whether container or item)
                    flexiPanel.RootContent = singleChild;
                }
                // General container child case
                else if (parentContainer != null)
                {
                    // Replace current container with singleChild in parent container
#if DEBUG
                    Debug.WriteLine($"[FlexiPaneManager] Replacing container with its single child in parent container");
#endif
                    
                    // Detach child from container
                    container.FirstChild = null!;
                    container.SecondChild = null!;
                    
                    // Replace in parent
                    if (parentContainer.FirstChild == container)
                    {
                        parentContainer.FirstChild = singleChild;
                    }
                    else if (parentContainer.SecondChild == container)
                    {
                        parentContainer.SecondChild = singleChild;
                    }
                }
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handle panel close completion event
        /// </summary>
        private static void OnPaneClosed(object? sender, PaneClosedEventArgs e)
        {
            // Disconnect events
            if (e.Pane != null)
            {
                DisconnectPaneEvents(e.Pane);
            }
        }

        #endregion
    }
}