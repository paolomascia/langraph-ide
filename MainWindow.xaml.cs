using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LangraphIDE.Controls;
using LangraphIDE.Models;
using LangraphIDE.Services;
using LangraphIDE.Windows;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace LangraphIDE
{
    public partial class MainWindow : Window
    {
        private List<NodeCategory> _nodeCategories = new();
        private string? _currentFilePath;
        private AgentNode? _selectedNode;
        private readonly PythonCodeGenerator _codeGenerator = new();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadNodeDefinitions();
            UpdateCodePreview();
        }

        private void LoadNodeDefinitions()
        {
            try
            {
                var dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "nodes.json");
                if (File.Exists(dataPath))
                {
                    var json = File.ReadAllText(dataPath);
                    _nodeCategories = JsonConvert.DeserializeObject<List<NodeCategory>>(json) ?? new();

                    foreach (var category in _nodeCategories)
                    {
                        foreach (var node in category.Nodes)
                        {
                            node.CategoryName = category.Category;
                            if (string.IsNullOrEmpty(node.DisplayName))
                                node.DisplayName = node.Name;
                        }
                    }

                    NodeCategoriesTree.ItemsSource = _nodeCategories;

                    foreach (var item in NodeCategoriesTree.Items)
                    {
                        var container = NodeCategoriesTree.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                        if (container != null)
                            container.IsExpanded = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading node definitions: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #region Node Library Drag & Drop

        private Point _dragStartPoint;
        private bool _isDragging;

        private void NodeCategoriesTree_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void NodeCategoriesTree_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !_isDragging)
            {
                var currentPoint = e.GetPosition(null);
                var diff = _dragStartPoint - currentPoint;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    var treeViewItem = FindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource);
                    if (treeViewItem?.DataContext is NodeDefinition nodeDefinition)
                    {
                        _isDragging = true;
                        var data = new DataObject(typeof(NodeDefinition), nodeDefinition);
                        DragDrop.DoDragDrop(treeViewItem, data, DragDropEffects.Copy);
                        _isDragging = false;
                    }
                }
            }
        }

        private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T ancestor)
                    return ancestor;
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        #endregion

        #region Canvas Events

        private void AgentCanvas_NodeSelected(object? sender, AgentNode node)
        {
            _selectedNode = node;
            ShowNodeProperties(node);
            UpdateCodePreview();
        }

        private void AgentCanvas_SelectionCleared(object? sender, EventArgs e)
        {
            _selectedNode = null;
            HideNodeProperties();
        }

        private void AgentCanvas_GenerateCodeRequested(object? sender, EventArgs e)
        {
            UpdateCodePreview();
            ExportCode();
        }

        #endregion

        #region Properties Panel

        private void ShowNodeProperties(AgentNode node)
        {
            NoSelectionText.Visibility = Visibility.Collapsed;
            NodePropertiesPanel.Visibility = Visibility.Visible;

            SelectedNodeName.Text = node.NodeDefinition.GetDisplayName();
            SelectedNodeType.Text = $"Type: {node.NodeDefinition.Type}";

            ParametersPanel.Children.Clear();

            foreach (var param in node.NodeDefinition.Parameters)
            {
                var paramPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };

                var label = new TextBlock
                {
                    Text = param.GetDisplayName() + (param.Required ? " *" : ""),
                    Foreground = (System.Windows.Media.Brush)FindResource("TextPrimaryBrush"),
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 0, 4)
                };
                paramPanel.Children.Add(label);

                if (!string.IsNullOrEmpty(param.Description))
                {
                    var desc = new TextBlock
                    {
                        Text = param.Description,
                        Foreground = (System.Windows.Media.Brush)FindResource("TextSecondaryBrush"),
                        FontSize = 10,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 4)
                    };
                    paramPanel.Children.Add(desc);
                }

                FrameworkElement inputControl;

                if (param.Options != null && param.Options.Any())
                {
                    var combo = new ComboBox
                    {
                        Tag = param.Name,
                        ItemsSource = param.Options
                    };

                    if (node.ParameterValues.TryGetValue(param.Name, out var value))
                        combo.SelectedItem = value;
                    else if (param.Default != null)
                        combo.SelectedItem = param.Default.ToString();

                    combo.SelectionChanged += (s, e) =>
                    {
                        if (combo.SelectedItem != null)
                        {
                            node.ParameterValues[param.Name] = combo.SelectedItem.ToString()!;
                            UpdateCodePreview();
                        }
                    };
                    inputControl = combo;
                }
                else if (param.Type == "boolean" || param.Input?.Type == "checkbox")
                {
                    var check = new CheckBox
                    {
                        Tag = param.Name,
                        Content = param.GetDisplayName(),
                        Foreground = (System.Windows.Media.Brush)FindResource("TextPrimaryBrush")
                    };

                    if (node.ParameterValues.TryGetValue(param.Name, out var value))
                        check.IsChecked = value.ToLower() == "true";
                    else if (param.Default != null)
                        check.IsChecked = param.Default.ToString()?.ToLower() == "true";

                    check.Checked += (s, e) =>
                    {
                        node.ParameterValues[param.Name] = param.Input?.TrueValue ?? "True";
                        UpdateCodePreview();
                    };
                    check.Unchecked += (s, e) =>
                    {
                        node.ParameterValues[param.Name] = param.Input?.FalseValue ?? "False";
                        UpdateCodePreview();
                    };
                    inputControl = check;
                }
                else if (param.Multiline)
                {
                    var paramNameLower = param.Name.ToLower();
                    string editorMode = (paramNameLower == "code" || paramNameLower == "routing_logic" ||
                                         paramNameLower == "updates" || paramNameLower == "condition")
                        ? "python" : "text";

                    var multilinePanel = new StackPanel();

                    var textBox = new TextBox
                    {
                        Tag = param.Name,
                        AcceptsReturn = true,
                        TextWrapping = TextWrapping.Wrap,
                        MinHeight = 80,
                        MaxHeight = 200,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                        FontSize = 11
                    };

                    if (node.ParameterValues.TryGetValue(param.Name, out var value))
                        textBox.Text = value;
                    else if (param.Default != null)
                        textBox.Text = param.Default.ToString();

                    textBox.TextChanged += (s, e) =>
                    {
                        node.ParameterValues[param.Name] = textBox.Text;
                        UpdateCodePreview();
                    };

                    multilinePanel.Children.Add(textBox);

                    var capturedTextBox = textBox;
                    var capturedParam = param;
                    var capturedMode = editorMode;

                    var editButton = new Button
                    {
                        Content = editorMode == "python" ? "Edit Code..." : "Edit...",
                        Margin = new Thickness(0, 4, 0, 0),
                        Padding = new Thickness(8, 4, 8, 4),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Style = (Style)FindResource("ToolbarButton")
                    };

                    editButton.Click += (s, e) =>
                    {
                        var dialog = new EditorDialog(
                            capturedParam.GetDisplayName(),
                            capturedTextBox.Text,
                            capturedMode)
                        {
                            Owner = this
                        };

                        if (dialog.ShowDialog() == true)
                        {
                            capturedTextBox.Text = dialog.EditedText;
                        }
                    };

                    multilinePanel.Children.Add(editButton);
                    inputControl = multilinePanel;
                }
                else
                {
                    var textBox = new TextBox { Tag = param.Name };

                    if (node.ParameterValues.TryGetValue(param.Name, out var value))
                        textBox.Text = value;
                    else if (param.Default != null)
                        textBox.Text = param.Default.ToString();

                    textBox.TextChanged += (s, e) =>
                    {
                        node.ParameterValues[param.Name] = textBox.Text;
                        UpdateCodePreview();
                    };
                    inputControl = textBox;
                }

                paramPanel.Children.Add(inputControl);
                ParametersPanel.Children.Add(paramPanel);
            }
        }

        private void HideNodeProperties()
        {
            NoSelectionText.Visibility = Visibility.Visible;
            NodePropertiesPanel.Visibility = Visibility.Collapsed;
            ParametersPanel.Children.Clear();
        }

        #endregion

        #region Code Generation

        private void UpdateCodePreview()
        {
            try
            {
                var graphData = AgentCanvas.ToGraphData();
                var code = _codeGenerator.Generate(graphData);
                CodePreviewBox.Text = code.FullCode;
            }
            catch (Exception ex)
            {
                CodePreviewBox.Text = $"# Error generating code:\n# {ex.Message}";
            }
        }

        private void ExportCode()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Python Files (*.py)|*.py|All Files (*.*)|*.*",
                DefaultExt = ".py",
                FileName = "agent_graph.py"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var graphData = AgentCanvas.ToGraphData();
                    var code = _codeGenerator.Generate(graphData);
                    File.WriteAllText(dialog.FileName, code.FullCode);
                    MessageBox.Show("Code exported successfully!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting code: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnCopyCode_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(CodePreviewBox.Text))
            {
                Clipboard.SetText(CodePreviewBox.Text);
            }
        }

        #endregion

        #region Menu Actions

        private void Menu_NewAgent_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Create a new agent? Unsaved changes will be lost.",
                "New Agent", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                AgentCanvas.Clear();
                _currentFilePath = null;
                Title = "Langraph IDE - Visual Editor";
                UpdateCodePreview();
            }
        }

        private void Menu_Open_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Agent Graph Files (*.agent.json)|*.agent.json|All Files (*.*)|*.*",
                DefaultExt = ".agent.json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var json = File.ReadAllText(dialog.FileName);
                    var graphData = JsonConvert.DeserializeObject<AgentGraphData>(json);

                    if (graphData != null)
                    {
                        LoadGraph(graphData);
                        _currentFilePath = dialog.FileName;
                        Title = $"Langraph IDE - {Path.GetFileName(dialog.FileName)}";
                        UpdateCodePreview();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening file: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Menu_Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                Menu_SaveAs_Click(sender, e);
                return;
            }

            SaveGraphToFile(_currentFilePath);
        }

        private void Menu_SaveAs_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Agent Graph Files (*.agent.json)|*.agent.json|All Files (*.*)|*.*",
                DefaultExt = ".agent.json",
                FileName = string.IsNullOrEmpty(_currentFilePath)
                    ? "agent.agent.json"
                    : Path.GetFileName(_currentFilePath)
            };

            if (dialog.ShowDialog() == true)
            {
                SaveGraphToFile(dialog.FileName);
                _currentFilePath = dialog.FileName;
                Title = $"Langraph IDE - {Path.GetFileName(dialog.FileName)}";
            }
        }

        private void Menu_Import_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Agent Graph Files (*.agent.json)|*.agent.json|All Files (*.*)|*.*",
                DefaultExt = ".agent.json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var json = File.ReadAllText(dialog.FileName);
                    var graphData = JsonConvert.DeserializeObject<AgentGraphData>(json);

                    if (graphData != null)
                    {
                        LoadGraph(graphData);
                        _currentFilePath = dialog.FileName;
                        Title = $"Langraph IDE - {Path.GetFileName(dialog.FileName)}";
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error importing file: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Menu_ExportGraph_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Agent Graph Files (*.agent.json)|*.agent.json|All Files (*.*)|*.*",
                DefaultExt = ".agent.json"
            };

            if (dialog.ShowDialog() == true)
            {
                SaveGraphToFile(dialog.FileName);
            }
        }

        private void LoadGraph(AgentGraphData graphData)
        {
            AgentCanvas.Clear();
            AgentCanvas.Metadata = graphData.Metadata;

            var nodeMap = new Dictionary<string, AgentNode>();

            foreach (var nodeData in graphData.Nodes)
            {
                var definition = FindNodeDefinition(nodeData.Type.ToString());
                if (definition != null)
                {
                    var node = AgentCanvas.AddNode(definition, new Point(nodeData.X, nodeData.Y));
                    node.SetNodeId(nodeData.Id);
                    foreach (var kv in nodeData.Parameters)
                        node.ParameterValues[kv.Key] = kv.Value;
                    node.CustomPythonCode = nodeData.PythonCode;
                    node.PromptTemplate = nodeData.PromptTemplate;
                    nodeMap[nodeData.Id] = node;
                }
            }

            foreach (var edgeData in graphData.Edges)
            {
                if (nodeMap.TryGetValue(edgeData.FromNodeId, out var fromNode) &&
                    nodeMap.TryGetValue(edgeData.ToNodeId, out var toNode))
                {
                    AgentCanvas.CreateConnection(fromNode, edgeData.FromConnector,
                        toNode, edgeData.ToConnector, edgeData.Condition);
                }
            }

            Dispatcher.InvokeAsync(new Action(() =>
            {
                ((Controls.AgentCanvas)FindName("AgentCanvas"))?.UpdateAllConnections();
            }), System.Windows.Threading.DispatcherPriority.Loaded);

            UpdateCodePreview();
        }

        private NodeDefinition? FindNodeDefinition(string typeName)
        {
            foreach (var category in _nodeCategories)
            {
                var node = category.Nodes.FirstOrDefault(n =>
                    n.Type.Equals(typeName, StringComparison.OrdinalIgnoreCase) ||
                    n.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));
                if (node != null) return node;
            }
            return _nodeCategories.FirstOrDefault()?.Nodes.FirstOrDefault();
        }

        private void SaveGraphToFile(string filePath)
        {
            try
            {
                var graphData = AgentCanvas.ToGraphData();
                graphData.Metadata.ModifiedAt = DateTime.Now;
                var json = JsonConvert.SerializeObject(graphData, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving file: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Menu_ExportCode_Click(object sender, RoutedEventArgs e) => ExportCode();
        private void Menu_Exit_Click(object sender, RoutedEventArgs e) => Close();

        private void Menu_Undo_Click(object sender, RoutedEventArgs e) => AgentCanvas.Undo();
        private void Menu_Redo_Click(object sender, RoutedEventArgs e) => AgentCanvas.Redo();
        private void Menu_Cut_Click(object sender, RoutedEventArgs e)
        {
            AgentCanvas.CopySelected();
            AgentCanvas.DeleteSelected();
        }
        private void Menu_Copy_Click(object sender, RoutedEventArgs e) => AgentCanvas.CopySelected();
        private void Menu_Paste_Click(object sender, RoutedEventArgs e) => AgentCanvas.Paste();
        private void Menu_Delete_Click(object sender, RoutedEventArgs e) => AgentCanvas.DeleteSelected();
        private void Menu_SelectAll_Click(object sender, RoutedEventArgs e) => AgentCanvas.SelectAll();

        private void Menu_TogglePanel_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is string panelName)
            {
                var panel = panelName switch
                {
                    "NodesPanel" => NodesPanel,
                    "PropertiesPanel" => PropertiesPanel,
                    _ => null
                };

                if (panel != null)
                {
                    panel.Visibility = menuItem.IsChecked ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        private void Menu_ZoomIn_Click(object sender, RoutedEventArgs e) { }
        private void Menu_ZoomOut_Click(object sender, RoutedEventArgs e) { }
        private void Menu_ZoomReset_Click(object sender, RoutedEventArgs e) { }

        private void Menu_About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Langraph IDE\n\nVisual editor for creating Langraph AI agents.\n\nVersion 1.0",
                "About", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Search

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = SearchBox.Text.ToLower();

            SearchPlaceholder.Visibility = string.IsNullOrEmpty(SearchBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;

            if (string.IsNullOrEmpty(searchText))
            {
                NodeCategoriesTree.ItemsSource = _nodeCategories;
                return;
            }

            var filtered = _nodeCategories
                .Select(c => new NodeCategory
                {
                    Category = c.Category,
                    Description = c.Description,
                    Icon = c.Icon,
                    Color = c.Color,
                    Nodes = c.Nodes.Where(n =>
                        n.Name.ToLower().Contains(searchText) ||
                        n.DisplayName.ToLower().Contains(searchText) ||
                        n.Description.ToLower().Contains(searchText)).ToList()
                })
                .Where(c => c.Nodes.Any())
                .ToList();

            NodeCategoriesTree.ItemsSource = filtered;
        }

        #endregion

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                switch (e.Key)
                {
                    case Key.N:
                        Menu_NewAgent_Click(sender, e);
                        e.Handled = true;
                        break;
                    case Key.O:
                        Menu_Open_Click(sender, e);
                        e.Handled = true;
                        break;
                    case Key.S:
                        Menu_Save_Click(sender, e);
                        e.Handled = true;
                        break;
                }
            }
        }
    }
}
