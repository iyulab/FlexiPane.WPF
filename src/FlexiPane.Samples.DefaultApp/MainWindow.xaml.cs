using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FlexiPane.Controls;
using FlexiPane.Events;
using FlexiPane.Serialization;
using Microsoft.Win32;

namespace FlexiPane.Samples.DefaultApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int _paneCounter = 1;
        private readonly Random _random = new();
        private readonly Dictionary<string, Func<UIElement>> _contentFactories;

        public MainWindow()
        {
            InitializeComponent();
            
            // Initialize content factories
            _contentFactories = new Dictionary<string, Func<UIElement>>();
            SetupContentFactories();
            
#if DEBUG
            Debug.WriteLine($"[MainWindow] Initializing - subscribing to events");
#endif
            
            // Subscribe to FlexiPanel events
            FlexiPanel.AddHandler(FlexiPanel.PaneSplitRequestedEvent, 
                new PaneSplitRequestedEventHandler(OnFlexiPanelPaneSplitRequested), false);
            
            FlexiPanel.LastPaneClosing += OnLastPaneClosing;
            FlexiPanel.PaneClosing += OnFlexiPanelPaneClosing;
            FlexiPanel.NewPaneCreated += OnNewPaneCreated;
            FlexiPanel.SplitModeChanged += OnSplitModeChanged;
            
            // Initialize on load
            this.Loaded += MainWindow_Loaded;
        }
        
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
#if DEBUG
                Debug.WriteLine($"[MainWindow] === INITIAL STATE CHECK ===");
                Debug.WriteLine($"[MainWindow] FlexiPanel is null? {FlexiPanel == null}");
                
                if (FlexiPanel != null)
                {
                    Debug.WriteLine($"[MainWindow] FlexiPanel IsSplitModeActive: {FlexiPanel.IsSplitModeActive}");
                    Debug.WriteLine($"[MainWindow] FlexiPanel ShowCloseButtons: {FlexiPanel.ShowCloseButtons}");
                    Debug.WriteLine($"[MainWindow] About to call CountTotalPanes...");
                    var paneCount = FlexiPanel.CountTotalPanes();
                    Debug.WriteLine($"[MainWindow] Total panes: {paneCount}");
                }
                else
                {
                    Debug.WriteLine($"[MainWindow] ERROR: FlexiPanel is null!");
                }
#endif
                
                // Set initial content if empty
                if (FlexiPanel != null && FlexiPanel.RootContent == null)
                {
                    Debug.WriteLine($"[MainWindow] Creating initial content...");
                    CreateInitialContent();
                    Debug.WriteLine($"[MainWindow] Initial content created");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MainWindow] EXCEPTION in MainWindow_Loaded: {ex.GetType().Name}");
                Debug.WriteLine($"[MainWindow] Message: {ex.Message}");
                Debug.WriteLine($"[MainWindow] StackTrace: {ex.StackTrace}");
                MessageBox.Show($"Error during initialization:\n\n{ex.Message}\n\nStack:\n{ex.StackTrace}", 
                    "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Content Factories Setup

        private void SetupContentFactories()
        {
            // Register different content types
            _contentFactories["editor"] = CreateEditorContent;
            _contentFactories["terminal"] = CreateTerminalContent;
            _contentFactories["explorer"] = CreateExplorerContent;
            _contentFactories["output"] = CreateOutputContent;
            _contentFactories["properties"] = CreatePropertiesContent;
            _contentFactories["welcome"] = CreateWelcomeContent;
            _contentFactories["default"] = CreateDefaultContent;

            // Register with FlexiPanel's serializer
            foreach (var factory in _contentFactories)
            {
                FlexiPanel.RegisterContentCreator(factory.Key, (paneInfo) =>
                {
                    if (_contentFactories.TryGetValue(paneInfo.ContentKey ?? "default", out var creator))
                    {
                        var content = creator();
                        
                        // Tag the content for future serialization
                        if (content is FrameworkElement fe)
                        {
                            fe.Tag = paneInfo.ContentKey;
                        }
                        
                        return content;
                    }
                    return CreateDefaultContent();
                });
            }

            // Set default creator
            FlexiPanel.SetDefaultContentCreator((paneInfo) => CreateDefaultContent());
        }

        #endregion

        #region Content Creation Methods

        private UIElement CreateEditorContent()
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
                Text = "📝 Code Editor",
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 10)
            });

            var editor = new TextBox
            {
                Text = @"// Sample Code
public class FlexiPaneDemo
{
    public void Main()
    {
        Console.WriteLine(""Hello FlexiPane!"");
    }
}",
                Background = new SolidColorBrush(Color.FromRgb(20, 20, 20)),
                Foreground = Brushes.LightGreen,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                MinHeight = 150,
                AcceptsReturn = true,
                AcceptsTab = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            stack.Children.Add(editor);
            border.Child = stack;
            border.Tag = "editor"; // Tag for serialization
            return border;
        }

        private UIElement CreateTerminalContent()
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
                Text = "💻 Terminal",
                Foreground = Brushes.Lime,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 10)
            });

            var output = new TextBox
            {
                Text = @"> FlexiPane Terminal v1.0
> Ready for commands...
> _",
                Background = Brushes.Black,
                Foreground = Brushes.Lime,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                MinHeight = 100,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            stack.Children.Add(output);
            border.Child = stack;
            border.Tag = "terminal";
            return border;
        }

        private UIElement CreateExplorerContent()
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
                Text = "📁 File Explorer",
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 10)
            });

            var treeView = new TreeView
            {
                Background = Brushes.Transparent,
                Foreground = Brushes.LightGray,
                BorderThickness = new Thickness(0)
            };

            var project = new TreeViewItem 
            { 
                Header = "📦 FlexiPane.WPF",
                IsExpanded = true,
                Foreground = Brushes.LightGray
            };
            
            var src = new TreeViewItem { Header = "📁 src", IsExpanded = true };
            src.Items.Add(new TreeViewItem { Header = "📄 FlexiPanel.cs" });
            src.Items.Add(new TreeViewItem { Header = "📄 FlexiPaneItem.cs" });
            src.Items.Add(new TreeViewItem { Header = "📄 FlexiPaneContainer.cs" });
            
            var docs = new TreeViewItem { Header = "📁 docs" };
            docs.Items.Add(new TreeViewItem { Header = "📄 README.md" });
            
            project.Items.Add(src);
            project.Items.Add(docs);
            project.Items.Add(new TreeViewItem { Header = "📁 tests" });
            
            treeView.Items.Add(project);
            stack.Children.Add(treeView);
            border.Child = stack;
            border.Tag = "explorer";
            return border;
        }

        private UIElement CreateOutputContent()
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
                Text = "📋 Output",
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 10)
            });

            var output = new TextBox
            {
                Text = @"Build started...
========== Build: 1 succeeded, 0 failed, 0 up-to-date, 0 skipped ==========
Build completed successfully.
FlexiPane.WPF ready.",
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                Foreground = Brushes.LightGray,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                MinHeight = 80,
                IsReadOnly = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            stack.Children.Add(output);
            border.Child = stack;
            border.Tag = "output";
            return border;
        }

        private UIElement CreatePropertiesContent()
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
                Text = "⚙️ Properties",
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 10)
            });

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Add property rows
            AddPropertyRow(grid, 0, "Name:", "FlexiPanel");
            AddPropertyRow(grid, 1, "Type:", "Control");
            AddPropertyRow(grid, 2, "Version:", "1.0.0");
            AddPropertyRow(grid, 3, "Status:", "Active");

            stack.Children.Add(grid);
            border.Child = stack;
            border.Tag = "properties";
            return border;
        }

        private void AddPropertyRow(Grid grid, int row, string label, string value)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            var labelBlock = new TextBlock
            {
                Text = label,
                Foreground = Brushes.LightGray,
                Margin = new Thickness(0, 2, 5, 2)
            };
            Grid.SetRow(labelBlock, row);
            Grid.SetColumn(labelBlock, 0);
            
            var valueBox = new TextBox
            {
                Text = value,
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.DarkGray,
                Margin = new Thickness(0, 2, 0, 2)
            };
            Grid.SetRow(valueBox, row);
            Grid.SetColumn(valueBox, 1);
            
            grid.Children.Add(labelBlock);
            grid.Children.Add(valueBox);
        }

        private UIElement CreateWelcomeContent()
        {
            var border = new Border
            {
                Background = new LinearGradientBrush(
                    Color.FromRgb(50, 50, 80),
                    Color.FromRgb(30, 30, 50),
                    90),
                BorderBrush = Brushes.DarkBlue,
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(5)
            };

            var stack = new StackPanel 
            { 
                Margin = new Thickness(20),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            
            stack.Children.Add(new TextBlock
            {
                Text = "🎯 Welcome to FlexiPane",
                Foreground = Brushes.White,
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            stack.Children.Add(new TextBlock
            {
                Text = "Dynamic Panel Splitter for WPF",
                Foreground = Brushes.LightGray,
                FontSize = 16,
                Margin = new Thickness(0, 0, 0, 30),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            var features = new TextBlock
            {
                Text = "✓ Split panels vertically or horizontally\n" +
                       "✓ Save and load layouts\n" +
                       "✓ Visual split mode\n" +
                       "✓ Fully customizable",
                Foreground = Brushes.LightGreen,
                FontSize = 14,
                LineHeight = 24,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            
            stack.Children.Add(features);
            border.Child = stack;
            border.Tag = "welcome";
            return border;
        }

        private UIElement CreateDefaultContent()
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
                Text = $"Pane #{_paneCounter++}",
                Foreground = Brushes.White,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            });

            stack.Children.Add(new TextBlock
            {
                Text = $"Created at {DateTime.Now:HH:mm:ss}",
                Foreground = Brushes.LightGray,
                FontSize = 12
            });

            border.Child = stack;
            border.Tag = "default";
            return border;
        }

        private void CreateInitialContent()
        {
            var welcomePane = new FlexiPaneItem
            {
                Content = CreateWelcomeContent(),
                Tag = "welcome"
            };
            
            FlexiPanel.RootContent = welcomePane;
        }

        #endregion

        #region Event Handlers

        private void ToggleSplitModeButton_Click(object sender, RoutedEventArgs e)
        {
#if DEBUG
            Debug.WriteLine($"[MainWindow] ToggleSplitModeButton_Click");
#endif
            FlexiPanel.IsSplitModeActive = !FlexiPanel.IsSplitModeActive;
            // ShowCloseButtons is now automatically synced with IsSplitModeActive in FlexiPanel
            
#if DEBUG
            Debug.WriteLine($"[MainWindow] FlexiPanel split mode set to: {FlexiPanel.IsSplitModeActive}");
            Debug.WriteLine($"[MainWindow] FlexiPanel show close buttons auto-synced to: {FlexiPanel.ShowCloseButtons}");
#endif
            
            UpdateUI();
        }

        private void SplitVerticalButton_Click(object sender, RoutedEventArgs e)
        {
            FlexiPanel.SplitSelectedVertically(0.5, CreateDefaultContent());
        }

        private void SplitHorizontalButton_Click(object sender, RoutedEventArgs e)
        {
            FlexiPanel.SplitSelectedHorizontally(0.5, CreateDefaultContent());
        }

        private void SaveLayoutButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Layout files (*.flexilayout)|*.flexilayout|XML files (*.xml)|*.xml",
                DefaultExt = ".flexilayout",
                FileName = $"layout_{DateTime.Now:yyyyMMdd_HHmmss}.flexilayout"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    Console.WriteLine($"[SaveLayout] Starting save to: {dialog.FileName}");
                    
                    // Tag all panes with their content type before saving
                    TagPanesForSerialization();
                    Console.WriteLine("[SaveLayout] Tagged panes for serialization");
                    
                    FlexiPanel.SaveLayoutToFile(dialog.FileName);
                    Console.WriteLine($"[SaveLayout] Successfully saved layout to: {dialog.FileName}");
                    
                    MessageBox.Show($"Layout saved successfully!\n\nFile: {Path.GetFileName(dialog.FileName)}", 
                        "Save Layout", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SaveLayout] ERROR: {ex.GetType().Name}: {ex.Message}");
                    Console.WriteLine($"[SaveLayout] Stack Trace:\n{ex.StackTrace}");
                    
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"[SaveLayout] Inner Exception: {ex.InnerException.Message}");
                        Console.WriteLine($"[SaveLayout] Inner Stack Trace:\n{ex.InnerException.StackTrace}");
                    }
                    
                    var detailedMessage = $"Failed to save layout:\n\n{ex.Message}";
                    if (ex.InnerException != null)
                    {
                        detailedMessage += $"\n\nInner Exception:\n{ex.InnerException.Message}";
                    }
                    
                    MessageBox.Show(detailedMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoadLayoutButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Layout files (*.flexilayout)|*.flexilayout|XML files (*.xml)|*.xml",
                DefaultExt = ".flexilayout"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    Console.WriteLine($"[LoadLayout] Starting load from: {dialog.FileName}");
                    
                    FlexiPanel.LoadLayoutFromFile(dialog.FileName);
                    Console.WriteLine($"[LoadLayout] Successfully loaded layout from: {dialog.FileName}");
                    
                    MessageBox.Show($"Layout loaded successfully!\n\nFile: {Path.GetFileName(dialog.FileName)}", 
                        "Load Layout", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[LoadLayout] ERROR: {ex.GetType().Name}: {ex.Message}");
                    Console.WriteLine($"[LoadLayout] Stack Trace:\n{ex.StackTrace}");
                    
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"[LoadLayout] Inner Exception: {ex.InnerException.Message}");
                        Console.WriteLine($"[LoadLayout] Inner Stack Trace:\n{ex.InnerException.StackTrace}");
                    }
                    
                    var detailedMessage = $"Failed to load layout:\n\n{ex.Message}";
                    if (ex.InnerException != null)
                    {
                        detailedMessage += $"\n\nInner Exception:\n{ex.InnerException.Message}";
                    }
                    
                    MessageBox.Show(detailedMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ClearLayoutButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "This will clear all panes and reset to initial state.\n\nAre you sure?",
                "Clear Layout",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                FlexiPanel.Clear();
                MessageBox.Show("Layout cleared successfully!", 
                    "Clear Layout", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void LoadIDELayoutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create IDE-style layout
                var layout = new FlexiPaneLayout
                {
                    Name = "IDE Layout",
                    RootNode = LayoutNode.CreateContainer(
                        SplitOrientation.Horizontal,
                        0.2,
                        LayoutNode.CreatePane("explorer"),
                        LayoutNode.CreateContainer(
                            SplitOrientation.Vertical,
                            0.7,
                            LayoutNode.CreateContainer(
                                SplitOrientation.Horizontal,
                                0.7,
                                LayoutNode.CreatePane("editor"),
                                LayoutNode.CreatePane("properties")
                            ),
                            LayoutNode.CreateContainer(
                                SplitOrientation.Horizontal,
                                0.6,
                                LayoutNode.CreatePane("output"),
                                LayoutNode.CreatePane("terminal")
                            )
                        )
                    )
                };

                // Serialize and load
                var serializer = new LayoutSerializer();
                foreach (var factory in _contentFactories)
                {
                    serializer.RegisterContentCreator(factory.Key, (paneInfo) => factory.Value());
                }
                
                var tempFile = Path.GetTempFileName();
                File.WriteAllText(tempFile, SerializeLayout(layout));
                FlexiPanel.LoadLayoutFromFile(tempFile);
                File.Delete(tempFile);
                
                MessageBox.Show("IDE layout loaded successfully!", 
                    "Load Preset", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load IDE layout:\n\n{ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSimpleLayoutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create simple two-pane layout
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

                // Serialize and load
                var tempFile = Path.GetTempFileName();
                File.WriteAllText(tempFile, SerializeLayout(layout));
                FlexiPanel.LoadLayoutFromFile(tempFile);
                File.Delete(tempFile);
                
                MessageBox.Show("Simple layout loaded successfully!", 
                    "Load Preset", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load simple layout:\n\n{ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnFlexiPanelPaneSplitRequested(object? sender, PaneSplitRequestedEventArgs e)
        {
#if DEBUG
            Debug.WriteLine($"[MainWindow] FlexiPanel PaneSplitRequested - IsVertical: {e.IsVerticalSplit}, Ratio: {e.SplitRatio}");
#endif
            
            // Provide new content for the split
            e.NewContent = CreateDefaultContent();
        }

        private void OnFlexiPanelPaneClosing(object? sender, PaneClosingEventArgs e)
        {
#if DEBUG
            Debug.WriteLine($"[MainWindow] FlexiPanel PaneClosing - Pane: {e.Pane?.GetHashCode()}");
#endif
        }

        private void OnLastPaneClosing(object? sender, LastPaneClosingEventArgs e)
        {
#if DEBUG
            Debug.WriteLine($"[MainWindow] LastPaneClosing - Last pane: {e.Pane?.GetHashCode()}");
#endif
            
            var result = MessageBox.Show(
                "This is the last remaining panel. Closing it will clear all content.\n\nDo you want to continue?", 
                "Confirm Last Panel Close", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
        }

        private void OnNewPaneCreated(object? sender, NewPaneCreatedEventArgs e)
        {
#if DEBUG
            Debug.WriteLine($"[MainWindow] New pane created - NewPane: {e.NewPane?.GetHashCode()}, SourcePane: {e.SourcePane?.GetHashCode()}");
#endif
            UpdateUI();
        }

        private void OnSplitModeChanged(object? sender, SplitModeChangedEventArgs e)
        {
#if DEBUG
            Debug.WriteLine($"[MainWindow] Split mode changed - IsActive: {e.IsActive}, OldValue: {e.OldValue}");
#endif
        }

        #endregion

        #region Helper Methods

        private void TagPanesForSerialization()
        {
            Console.WriteLine("[TagPanes] Starting to tag panes for serialization");
            // Walk through all panes and set their Tag based on content
            TagPaneRecursive(FlexiPanel.RootContent, 0);
            Console.WriteLine("[TagPanes] Completed tagging panes");
        }

        private void TagPaneRecursive(UIElement? element, int depth)
        {
            if (element == null) 
            {
                Console.WriteLine($"[TagPanes] {new string(' ', depth * 2)}Null element");
                return;
            }

            if (element is FlexiPaneItem paneItem)
            {
                // Try to determine content type from the content
                if (paneItem.Content is Border border && border.Tag is string tag)
                {
                    paneItem.Tag = tag;
                    Console.WriteLine($"[TagPanes] {new string(' ', depth * 2)}Tagged FlexiPaneItem with: {tag}");
                }
                else if (paneItem.Tag == null)
                {
                    paneItem.Tag = "default";
                    Console.WriteLine($"[TagPanes] {new string(' ', depth * 2)}Tagged FlexiPaneItem with default");
                }
                else
                {
                    Console.WriteLine($"[TagPanes] {new string(' ', depth * 2)}FlexiPaneItem already has tag: {paneItem.Tag}");
                }
            }
            else if (element is FlexiPaneContainer container)
            {
                Console.WriteLine($"[TagPanes] {new string(' ', depth * 2)}Found FlexiPaneContainer");
                TagPaneRecursive(container.FirstChild, depth + 1);
                TagPaneRecursive(container.SecondChild, depth + 1);
            }
            else
            {
                Console.WriteLine($"[TagPanes] {new string(' ', depth * 2)}Unknown element type: {element.GetType().Name}");
            }
        }

        private string SerializeLayout(FlexiPaneLayout layout)
        {
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(FlexiPaneLayout));
            using var writer = new StringWriter();
            serializer.Serialize(writer, layout);
            return writer.ToString();
        }

        private void UpdateUI()
        {
            var activePane = FlexiPanel.SelectedItem;

            SplitVerticalButton.IsEnabled = activePane?.CanSplit == true;
            SplitHorizontalButton.IsEnabled = activePane?.CanSplit == true;
            
            ToggleSplitModeButton.Content = FlexiPanel.IsSplitModeActive ? "Split Mode OFF" : "Split Mode ON";
        }

        #endregion
    }
}