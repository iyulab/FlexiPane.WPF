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
    /// FlexiPane 분할 및 관리 로직을 담당하는 정적 클래스
    /// </summary>
    public static class FlexiPaneManager
    {
        /// <summary>
        /// FlexiPaneItem을 분할하여 새로운 FlexiPaneContainer로 변환
        /// </summary>
        /// <param name="sourcePane">분할될 원본 패널</param>
        /// <param name="isVerticalSplit">true: 세로 분할 (위/아래), false: 가로 분할 (좌/우)</param>
        /// <param name="splitRatio">분할 비율 (0.1 ~ 0.9)</param>
        /// <param name="newContent">새 패널에 들어갈 콘텐츠</param>
        /// <returns>생성된 FlexiPaneContainer 또는 실패시 null</returns>
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

            // 분할 비율 검증
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

            // sourcePane의 직접 부모 찾기
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
                // 1. 새로운 FlexiPaneContainer 생성
                var container = new FlexiPaneContainer
                {
                    IsVerticalSplit = isVerticalSplit,
                    SplitRatio = splitRatio
                };

                // 2. 원본 패널의 속성 복사
                CopyCommonProperties(sourcePane, container);

                // 3. 새로운 패널 생성
                var newPane = new FlexiPaneItem();
                if (newContent != null)
                {
                    newPane.Content = newContent;
                }
                else
                {
                    // 기본 콘텐츠 생성
                    newPane.Content = CreateDefaultPaneContent();
                }

#if DEBUG
                Debug.WriteLine($"[FlexiPaneManager] Setting container children - First: {sourcePane.GetType().Name}, Second: {newPane.GetType().Name}");
#endif
                
                // 4. sourcePane을 기존 부모에서 제거 (logical parent 충돌 방지)
#if DEBUG
                Debug.WriteLine($"[FlexiPaneManager] Removing sourcePane from current parent to avoid logical parent conflicts");
#endif
                RemoveFromLogicalParent(sourcePane);
                
                // 5. 컨테이너에 패널들 설정
                container.FirstChild = sourcePane;
                container.SecondChild = newPane;

                // 6. 직접 부모에서 sourcePane을 container로 교체
#if DEBUG
                Debug.WriteLine($"[FlexiPaneManager] Replacing sourcePane with container in direct parent");
#endif
                ReplaceChild(directParent, sourcePane, container);

#if DEBUG
                Debug.WriteLine($"[FlexiPaneManager] Split process completed - Container created with {container.FirstChild?.GetType().Name} and {container.SecondChild?.GetType().Name}");
#endif

                // 6. 이벤트 연결
                ConnectPaneEvents(sourcePane);
                ConnectPaneEvents(newPane);

                // 7. 새 패널 생성 이벤트 발생
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
        /// FlexiPaneContainer에서 패널을 제거하고 필요시 구조 단순화
        /// splitting-mechanism.md의 제거 알고리즘을 따름
        /// </summary>
        /// <param name="containerToClose">닫힐 패널이 속한 컨테이너</param>
        /// <param name="paneToClose">닫힐 패널</param>
        /// <returns>성공 여부</returns>
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
                // 1. 남을 패널(형제) 결정
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

                // 2. 부모의 부모(grandParent) 찾기 - FlexiPanel 포함
                var grandParent = containerToClose.Parent;
                DependencyObject? actualGrandParent = grandParent; // 실제로 교체를 수행할 부모
                
                // Parent가 null인 경우 FlexiPanel의 RootContent일 수 있음
                if (grandParent == null)
                {
#if DEBUG
                    Debug.WriteLine($"[FlexiPaneManager] No direct parent found, searching for FlexiPanel via RootContent");
#endif
                    
                    // FlexiPanel의 RootContent로 설정된 경우 찾기
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
                // grandParent가 Grid인 경우, 실제 FlexiPaneContainer를 찾아야 함
                else if (grandParent is Grid grid)
                {
#if DEBUG
                    Debug.WriteLine($"[FlexiPaneManager] GrandParent is Grid, finding actual FlexiPaneContainer");
#endif
                    // Grid의 부모가 FlexiPaneContainer일 것임
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
                        actualGrandParent = grandParent; // Grid 자체를 사용
                    }
                }
                else
                {
                    actualGrandParent = grandParent;
                }

#if DEBUG
                Debug.WriteLine($"[FlexiPaneManager] Container grandParent: {grandParent.GetType().Name}, Actual grandParent: {actualGrandParent?.GetType().Name}");
#endif

                // 3. 먼저 형제를 컨테이너에서 분리
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

                // 4. 계층 구조 단순화: actualGrandParent에서 container를 sibling으로 교체
                ReplaceChild(actualGrandParent!, containerToClose, sibling);

                // 5. 리소스 정리
                DisconnectPaneEvents(paneToClose);
                containerToClose.FirstChild = null!;
                containerToClose.SecondChild = null!;

                // 6. 구조 단순화 확인 - 형제가 컨테이너인 경우에만 수행
                // (형제가 FlexiPaneItem인 경우는 이미 교체로 단순화 완료)
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
        /// UIElement의 직접 부모 찾기 (FlexiPanel 또는 FlexiPaneContainer)
        /// </summary>
        private static DependencyObject? FindDirectParent(UIElement element)
        {
            if (element == null) return null;

#if DEBUG
            Debug.WriteLine($"[FlexiPaneManager] FindDirectParent - Element: {element.GetType().Name}");
#endif

            // Logical Parent 우선 확인
            var logicalParent = LogicalTreeHelper.GetParent(element);
            if (logicalParent != null)
            {
#if DEBUG
                Debug.WriteLine($"[FlexiPaneManager] Logical parent found: {logicalParent.GetType().Name}");
#endif
                
                // FlexiPanel이나 FlexiPaneContainer를 찾을 때까지 올라감
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

            // Visual Parent로 대체 시도
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
        /// FlexiPaneItem의 직접 부모 FlexiPaneContainer를 찾기
        /// </summary>
        public static FlexiPaneContainer? FindDirectParentContainer(FlexiPaneItem pane)
        {
            if (pane == null) return null;

            // 먼저 직접 부모를 확인
            var directParent = pane.Parent;
#if DEBUG
            Debug.WriteLine($"[FlexiPaneManager] FindDirectParentContainer - Pane: {pane.GetHashCode()}, Direct parent: {directParent?.GetType().Name}");
#endif

            // 직접 부모가 Grid인 경우, Visual Tree를 사용해서 부모를 확인
            if (directParent is Grid grid)
            {
                // Logical Parent와 Visual Parent 모두 확인
                var logicalParent = LogicalTreeHelper.GetParent(grid);
                var visualParent = VisualTreeHelper.GetParent(grid);
                
#if DEBUG
                Debug.WriteLine($"[FlexiPaneManager] Direct parent is Grid, checking parents - Logical: {logicalParent?.GetType().Name}, Visual: {visualParent?.GetType().Name}");
#endif
                
                // Logical Parent 먼저 확인
                if (logicalParent is FlexiPaneContainer logicalContainer)
                {
#if DEBUG
                    Debug.WriteLine($"[FlexiPaneManager] Found logical FlexiPaneContainer - FirstChild: {logicalContainer.FirstChild?.GetType().Name}, SecondChild: {logicalContainer.SecondChild?.GetType().Name}");
#endif
                    
                    // 빈 컨테이너가 아닌 경우만 확인
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
                
                // Visual Parent 확인
                if (visualParent is FlexiPaneContainer visualContainer)
                {
#if DEBUG
                    Debug.WriteLine($"[FlexiPaneManager] Found visual FlexiPaneContainer - FirstChild: {visualContainer.FirstChild?.GetType().Name}, SecondChild: {visualContainer.SecondChild?.GetType().Name}");
#endif
                    
                    // 빈 컨테이너가 아닌 경우만 확인
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

            // Visual Tree와 Logical Tree를 모두 탐색
            var current = directParent;
            
            while (current != null)
            {
#if DEBUG
                Debug.WriteLine($"[FlexiPaneManager] Checking parent: {current.GetType().Name}");
#endif
                
                // FlexiPaneContainer를 찾은 경우
                if (current is FlexiPaneContainer container)
                {
#if DEBUG
                    Debug.WriteLine($"[FlexiPaneManager] Found FlexiPaneContainer - FirstChild: {container.FirstChild?.GetType().Name ?? "null"}, SecondChild: {container.SecondChild?.GetType().Name ?? "null"}");
#endif
                    
                    // 빈 컨테이너가 아니고 이 컨테이너가 우리 pane을 포함하는지 재귀적으로 확인
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

                // 다음 부모로 이동
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
        /// UIElement가 특정 FlexiPaneItem을 포함하는지 재귀적으로 확인
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
        /// 기본 패널 콘텐츠 생성
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
        /// 공통 속성 복사
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
        /// 부모에서 요소 제거
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
                    
                    // Grid에서 제거할 때 Grid attached properties 초기화
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
        /// 부모에서 자식 요소를 교체 (위치 정보 보존)
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
                        // Grid attached properties 백업
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
                            
                            // 새 요소에 위치 정보 적용
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
                        container.FirstChild = newChild;
                    else if (container.SecondChild == oldChild)
                        container.SecondChild = newChild;
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
        /// 부모에 요소 추가
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
                    // Grid인 경우 자식들의 위치가 겹치지 않도록 처리
                    if (panel is Grid grid)
                    {
                        // FlexiPaneContainer의 Grid는 자체적으로 레이아웃을 관리하므로 그대로 추가
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
                    // 첫 번째 빈 슬롯에 추가
                    if (container.FirstChild == null)
                        container.FirstChild = child;
                    else if (container.SecondChild == null)
                        container.SecondChild = child;
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
        /// 패널 이벤트 연결 (분할 관련만)
        /// </summary>
        public static void ConnectPaneEvents(FlexiPaneItem pane)
        {
            if (pane == null) return;

            // 분할 요청 이벤트만 처리 (닫기는 FlexiPanel에서 처리)
            pane.SplitRequested += OnPaneSplitRequested;
            pane.Closed += OnPaneClosed;
        }

        /// <summary>
        /// 패널 이벤트 연결 해제
        /// </summary>
        public static void DisconnectPaneEvents(FlexiPaneItem pane)
        {
            if (pane == null) return;

            pane.SplitRequested -= OnPaneSplitRequested;
            pane.Closed -= OnPaneClosed;
        }

        /// <summary>
        /// UIElement를 현재 논리적 부모에서 제거
        /// </summary>
        private static void RemoveFromLogicalParent(UIElement element)
        {
            if (element == null) return;

#if DEBUG
            Debug.WriteLine($"[FlexiPaneManager] RemoveFromLogicalParent - Element: {element.GetType().Name}");
#endif

            // Visual Tree와 Logical Tree를 통해 부모 찾기
            DependencyObject logicalParent = LogicalTreeHelper.GetParent(element);
            DependencyObject visualParent = VisualTreeHelper.GetParent(element);

#if DEBUG
            Debug.WriteLine($"[FlexiPaneManager] LogicalParent: {logicalParent?.GetType().Name ?? "null"}");
            Debug.WriteLine($"[FlexiPaneManager] VisualParent: {visualParent?.GetType().Name ?? "null"}");
#endif

            // Logical Parent 우선 처리
            if (logicalParent != null)
            {
#if DEBUG
                Debug.WriteLine($"[FlexiPaneManager] Removing from logical parent: {logicalParent.GetType().Name}");
#endif
                RemoveFromParent(element, logicalParent);
            }
            // Visual Parent가 다른 경우 처리
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
        /// 컨테이너가 비어있는지 확인
        /// </summary>
        private static bool IsEmptyContainer(FlexiPaneContainer container)
        {
            return container != null && 
                   container.FirstChild == null && 
                   container.SecondChild == null;
        }
        
        /// <summary>
        /// 컨테이너 내의 총 패널 개수 세기
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
        /// 구조 단순화 - splitting-mechanism.md 문서에 정의된 규칙에 따름
        /// 컨테이너에 하나의 자식만 남은 경우, 그 자식을 부모 레벨로 승격
        /// </summary>
        private static void SimplifyStructure(DependencyObject parent)
        {
            if (parent == null) return;
            
#if DEBUG
            Debug.WriteLine($"[FlexiPaneManager] SimplifyStructure - Parent: {parent.GetType().Name}");
#endif
            
            // FlexiPanel인 경우
            if (parent is FlexiPanel flexiPanel)
            {
                if (flexiPanel.RootContent is FlexiPaneContainer rootContainer)
                {
                    SimplifyContainer(rootContainer, flexiPanel, null);
                }
            }
            // FlexiPaneContainer인 경우
            else if (parent is FlexiPaneContainer parentContainer)
            {
                // 부모 컨테이너의 자식 컨테이너들 단순화
                if (parentContainer.FirstChild is FlexiPaneContainer firstContainer)
                {
                    SimplifyContainer(firstContainer, null, parentContainer);
                }
                if (parentContainer.SecondChild is FlexiPaneContainer secondContainer)
                {
                    SimplifyContainer(secondContainer, null, parentContainer);
                }
            }
            // Grid인 경우 (FlexiPaneContainer의 내부 구현)
            else if (parent is Grid)
            {
                // FlexiPanel을 찾아서 전체 구조 단순화
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
        /// 개별 컨테이너 단순화 - 하나의 자식만 있는 경우 그 자식을 승격
        /// </summary>
        private static void SimplifyContainer(FlexiPaneContainer container, FlexiPanel? flexiPanel, FlexiPaneContainer? parentContainer)
        {
            if (container == null) return;
            
#if DEBUG
            Debug.WriteLine($"[FlexiPaneManager] SimplifyContainer - FirstChild: {container.FirstChild?.GetType().Name ?? "null"}, SecondChild: {container.SecondChild?.GetType().Name ?? "null"}");
#endif
            
            // 빈 컨테이너인 경우 부모에서 제거
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
            
            // 먼저 자식 컨테이너들을 재귀적으로 단순화
            if (container.FirstChild is FlexiPaneContainer firstChildContainer)
            {
                SimplifyContainer(firstChildContainer, null, container);
                // 단순화 후 빈 컨테이너가 되었는지 확인
                if (IsEmptyContainer(firstChildContainer))
                {
                    container.FirstChild = null!;
                }
            }
            if (container.SecondChild is FlexiPaneContainer secondChildContainer)
            {
                SimplifyContainer(secondChildContainer, null, container);
                // 단순화 후 빈 컨테이너가 되었는지 확인
                if (IsEmptyContainer(secondChildContainer))
                {
                    container.SecondChild = null!;
                }
            }
            
            // 컨테이너에 하나의 자식만 있는지 확인
            UIElement? singleChild = null;
            if (container.FirstChild != null && container.SecondChild == null)
            {
                singleChild = container.FirstChild;
            }
            else if (container.SecondChild != null && container.FirstChild == null)
            {
                singleChild = container.SecondChild;
            }
            
            // 하나의 자식만 있는 경우 승격 처리
            if (singleChild != null)
            {
#if DEBUG
                Debug.WriteLine($"[FlexiPaneManager] Container has single child: {singleChild.GetType().Name}");
#endif
                
                // FlexiPanel의 RootContent인 경우
                if (flexiPanel != null && flexiPanel.RootContent == container)
                {
#if DEBUG
                    Debug.WriteLine($"[FlexiPaneManager] Promoting single child to RootContent for structure simplification");
#endif
                    // 컨테이너에서 자식 분리
                    container.FirstChild = null!;
                    container.SecondChild = null!;
                    // 단일 자식을 RootContent로 승격 (컨테이너든 아이템이든)
                    flexiPanel.RootContent = singleChild;
                }
                // 일반 컨테이너의 자식인 경우
                else if (parentContainer != null)
                {
                    // 부모 컨테이너에서 현재 컨테이너를 singleChild로 교체
#if DEBUG
                    Debug.WriteLine($"[FlexiPaneManager] Replacing container with its single child in parent container");
#endif
                    
                    // 컨테이너에서 자식 분리
                    container.FirstChild = null!;
                    container.SecondChild = null!;
                    
                    // 부모에서 교체
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
        /// 패널 분할 요청 이벤트 처리
        /// </summary>
        private static void OnPaneSplitRequested(object? sender, PaneSplitRequestedEventArgs e)
        {
#if DEBUG
            Debug.WriteLine($"[FlexiPaneManager] OnPaneSplitRequested - Sender: {sender?.GetType().Name}, Cancel: {e.Cancel}");
#endif

            if (sender is not FlexiPaneItem sourcePane || e.Cancel)
            {
#if DEBUG
                Debug.WriteLine($"[FlexiPaneManager] Split request ignored - ValidSender: {sender is FlexiPaneItem}, Cancel: {e.Cancel}");
#endif
                return;
            }

#if DEBUG
            Debug.WriteLine($"[FlexiPaneManager] Processing split request - IsVertical: {e.IsVerticalSplit}, Ratio: {e.SplitRatio:F2}");
#endif

            // 자동 분할 수행
            var result = SplitPane(sourcePane, e.IsVerticalSplit, e.SplitRatio, e.NewContent as UIElement);
            
            if (result == null)
            {
                // 분할 실패를 이벤트 아규먼트에 표시
                e.Cancel = true;
#if DEBUG
                Debug.WriteLine($"[FlexiPaneManager] Split failed - setting Cancel = true");
#endif
            }
            else
            {
#if DEBUG
                Debug.WriteLine($"[FlexiPaneManager] Split succeeded - Container created");
#endif
            }
        }


        /// <summary>
        /// 패널 닫기 완료 이벤트 처리
        /// </summary>
        private static void OnPaneClosed(object? sender, PaneClosedEventArgs e)
        {
            // 이벤트 연결 해제
            if (e.Pane != null)
            {
                DisconnectPaneEvents(e.Pane);
            }
        }

        #endregion
    }
}