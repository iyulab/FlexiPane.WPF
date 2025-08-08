using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FlexiPane.Controls;
using FlexiPane.Serialization;
using Microsoft.Win32;

namespace FlexiPane.Samples.DefaultApp
{
    /// <summary>
    /// Example of how to use the layout serialization system
    /// </summary>
    public class LayoutExample
    {
        private readonly FlexiPanel _flexiPanel;
        private readonly Dictionary<string, Func<UIElement>> _contentTypes;

        public LayoutExample(FlexiPanel flexiPanel)
        {
            _flexiPanel = flexiPanel;
            _contentTypes = new Dictionary<string, Func<UIElement>>();
            
            // Register default content types
            RegisterDefaultContentTypes();
            SetupContentFactories();
        }

        #region Content Type Registration

        private void RegisterDefaultContentTypes()
        {
            // Register different content types that can be recreated
            _contentTypes["editor"] = CreateEditorPane;
            _contentTypes["terminal"] = CreateTerminalPane;
            _contentTypes["explorer"] = CreateExplorerPane;
            _contentTypes["output"] = CreateOutputPane;
            _contentTypes["properties"] = CreatePropertiesPane;
        }

        private void SetupContentFactories()
        {
            // Register content creators with the FlexiPanel
            foreach (var contentType in _contentTypes)
            {
                FlexiPanel.RegisterContentCreator(contentType.Key, (paneInfo) =>
                {
                    // Create the appropriate content based on the key
                    if (_contentTypes.TryGetValue(paneInfo.ContentKey ?? "", out var creator))
                    {
                        var content = creator();
                        
                        // You can use paneInfo to restore additional state
                        if (content is FrameworkElement element)
                        {
                            element.Tag = paneInfo.Id; // Store pane ID for reference
                            
                            // Apply custom properties if needed
                            if (paneInfo.CustomProperties.TryGetValue("Title", out var title))
                            {
                                // Apply title if the content supports it
                                if (element is Panel panel && panel.Children.Count > 0)
                                {
                                    if (panel.Children[0] is TextBlock titleBlock)
                                    {
                                        titleBlock.Text = title;
                                    }
                                }
                            }
                        }
                        
                        return content;
                    }
                    
                    // Fallback content
                    return CreateDefaultPane(paneInfo);
                });
            }

            // Set default content creator for unregistered types
            FlexiPanel.SetDefaultContentCreator(CreateDefaultPane);
        }

        #endregion

        #region Content Creation Methods

        private UIElement CreateEditorPane()
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                BorderBrush = Brushes.DarkGray,
                BorderThickness = new Thickness(1)
            };

            var stack = new StackPanel { Margin = new Thickness(10) };
            
            stack.Children.Add(new TextBlock
            {
                Text = "Code Editor",
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            });

            stack.Children.Add(new TextBox
            {
                Text = "// Your code here...",
                Background = Brushes.Black,
                Foreground = Brushes.LightGreen,
                FontFamily = new FontFamily("Consolas"),
                MinHeight = 100,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            });

            border.Child = stack;
            return border;
        }

        private UIElement CreateTerminalPane()
        {
            var border = new Border
            {
                Background = Brushes.Black,
                BorderBrush = Brushes.Green,
                BorderThickness = new Thickness(1)
            };

            var stack = new StackPanel { Margin = new Thickness(10) };
            
            stack.Children.Add(new TextBlock
            {
                Text = "Terminal",
                Foreground = Brushes.Lime,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            });

            stack.Children.Add(new TextBox
            {
                Text = "> Ready for commands...",
                Background = Brushes.Black,
                Foreground = Brushes.Lime,
                FontFamily = new FontFamily("Consolas"),
                MinHeight = 80
            });

            border.Child = stack;
            return border;
        }

        private UIElement CreateExplorerPane()
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(37, 37, 38)),
                BorderBrush = Brushes.DarkGray,
                BorderThickness = new Thickness(1)
            };

            var stack = new StackPanel { Margin = new Thickness(10) };
            
            stack.Children.Add(new TextBlock
            {
                Text = "File Explorer",
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            });

            var treeView = new TreeView
            {
                Background = Brushes.Transparent,
                Foreground = Brushes.LightGray
            };

            var root = new TreeViewItem { Header = "Project", IsExpanded = true };
            root.Items.Add(new TreeViewItem { Header = "src/" });
            root.Items.Add(new TreeViewItem { Header = "docs/" });
            root.Items.Add(new TreeViewItem { Header = "tests/" });
            treeView.Items.Add(root);

            stack.Children.Add(treeView);
            border.Child = stack;
            return border;
        }

        private UIElement CreateOutputPane()
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(40, 40, 40)),
                BorderBrush = Brushes.DarkGray,
                BorderThickness = new Thickness(1)
            };

            var stack = new StackPanel { Margin = new Thickness(10) };
            
            stack.Children.Add(new TextBlock
            {
                Text = "Output",
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            });

            stack.Children.Add(new TextBox
            {
                Text = "Build output will appear here...",
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                Foreground = Brushes.LightGray,
                FontFamily = new FontFamily("Consolas"),
                MinHeight = 60,
                IsReadOnly = true
            });

            border.Child = stack;
            return border;
        }

        private UIElement CreatePropertiesPane()
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                BorderBrush = Brushes.DarkGray,
                BorderThickness = new Thickness(1)
            };

            var stack = new StackPanel { Margin = new Thickness(10) };
            
            stack.Children.Add(new TextBlock
            {
                Text = "Properties",
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            });

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var label1 = new TextBlock { Text = "Name:", Foreground = Brushes.LightGray, Margin = new Thickness(0, 2, 5, 2) };
            var value1 = new TextBox { Text = "Item1", Margin = new Thickness(0, 2, 0, 2) };
            Grid.SetColumn(value1, 1);

            var label2 = new TextBlock { Text = "Type:", Foreground = Brushes.LightGray, Margin = new Thickness(0, 2, 5, 2) };
            var value2 = new TextBox { Text = "File", Margin = new Thickness(0, 2, 0, 2) };
            Grid.SetRow(label2, 1);
            Grid.SetColumn(value2, 1);
            Grid.SetRow(value2, 1);

            grid.Children.Add(label1);
            grid.Children.Add(value1);
            grid.Children.Add(label2);
            grid.Children.Add(value2);

            stack.Children.Add(grid);
            border.Child = stack;
            return border;
        }

        private UIElement CreateDefaultPane(PaneInfo paneInfo)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1)
            };

            var stack = new StackPanel 
            { 
                Margin = new Thickness(10),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            
            stack.Children.Add(new TextBlock
            {
                Text = $"Pane ID: {paneInfo.Id}",
                Foreground = Brushes.White,
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 5)
            });

            if (!string.IsNullOrEmpty(paneInfo.ContentKey))
            {
                stack.Children.Add(new TextBlock
                {
                    Text = $"Content Key: {paneInfo.ContentKey}",
                    Foreground = Brushes.LightGray,
                    FontSize = 11
                });
            }

            stack.Children.Add(new TextBlock
            {
                Text = $"Path: {paneInfo.TreePath}",
                Foreground = Brushes.Gray,
                FontSize = 10,
                Margin = new Thickness(0, 5, 0, 0)
            });

            border.Child = stack;
            return border;
        }

        #endregion

        #region Save/Load Operations

        public void SaveLayoutToFile()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Layout files (*.flexilayout)|*.flexilayout|XML files (*.xml)|*.xml|All files (*.*)|*.*",
                DefaultExt = ".flexilayout",
                FileName = $"layout_{DateTime.Now:yyyyMMdd_HHmmss}.flexilayout"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Before saving, tag all panes with their content types
                    TagPanesWithContentTypes();
                    
                    _flexiPanel.SaveLayoutToFile(dialog.FileName);
                    MessageBox.Show($"Layout saved successfully to:\n{dialog.FileName}", 
                        "Save Layout", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save layout:\n{ex.Message}", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void LoadLayoutFromFile()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Layout files (*.flexilayout)|*.flexilayout|XML files (*.xml)|*.xml|All files (*.*)|*.*",
                DefaultExt = ".flexilayout"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _flexiPanel.LoadLayoutFromFile(dialog.FileName);
                    MessageBox.Show($"Layout loaded successfully from:\n{dialog.FileName}", 
                        "Load Layout", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load layout:\n{ex.Message}", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void TagPanesWithContentTypes()
        {
            // This method would traverse the panel tree and set appropriate tags
            // based on the content. For this example, we'll use a simple approach
            // In a real application, you'd have a more sophisticated way to identify content types
            
            // Example: Tag panes based on their content
            // This would be called before saving to ensure content types are preserved
        }

        #endregion

        #region Preset Layouts

        public void LoadIDELayout()
        {
            // Create a typical IDE layout programmatically
            var layout = new FlexiPaneLayout
            {
                Name = "IDE Layout",
                RootNode = LayoutNode.CreateContainer(
                    SplitOrientation.Horizontal,
                    0.2, // 20% for explorer
                    LayoutNode.CreatePane("explorer"),
                    LayoutNode.CreateContainer(
                        SplitOrientation.Vertical,
                        0.7, // 70% for editor area
                        LayoutNode.CreateContainer(
                            SplitOrientation.Horizontal,
                            0.7, // 70% for main editor
                            LayoutNode.CreatePane("editor"),
                            LayoutNode.CreatePane("properties")
                        ),
                        LayoutNode.CreateContainer(
                            SplitOrientation.Horizontal,
                            0.5, // 50/50 for output and terminal
                            LayoutNode.CreatePane("output"),
                            LayoutNode.CreatePane("terminal")
                        )
                    )
                )
            };

            var xml = new LayoutSerializer().SaveLayout(_flexiPanel);
            _flexiPanel.LoadLayout(xml);
        }

        public void LoadSimpleLayout()
        {
            var layout = new FlexiPaneLayout
            {
                Name = "Simple Layout",
                RootNode = LayoutNode.CreateContainer(
                    SplitOrientation.Horizontal,
                    0.5,
                    LayoutNode.CreatePane("editor"),
                    LayoutNode.CreatePane("output")
                )
            };

            var serializer = new LayoutSerializer();
            foreach (var contentType in _contentTypes)
            {
                serializer.RegisterContentCreator(contentType.Key, (paneInfo) => contentType.Value());
            }

            var xml = serializer.SaveLayout(_flexiPanel);
            _flexiPanel.LoadLayout(xml);
        }

        #endregion
    }
}