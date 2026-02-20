using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using LangraphIDE.Models;
using LangraphIDE.Windows;

namespace LangraphIDE.Controls
{
    public partial class AgentCanvas : UserControl
    {
        private readonly List<AgentNode> _nodes = new();
        private readonly List<NodeConnection> _connections = new();
        private readonly Stack<IUndoableAction> _undoStack = new();
        private readonly Stack<IUndoableAction> _redoStack = new();

        private readonly HashSet<AgentNode> _selectedNodes = new();
        private NodeConnection? _selectedConnection;
        private bool _isDrawingConnection;
        private AgentNode? _connectionStartNode;
        private string? _connectionStartConnector;
        private Path? _tempConnectionPath;

        private bool _isPanning;
        private Point _panStartPoint;
        private Point _scrollStartOffset;

        private bool _isSelecting;
        private Point _selectionStartPoint;

        private List<ClipboardNodeData>? _clipboard;
        private List<ClipboardEdgeData>? _clipboardConnections;

        private double _zoomLevel = 1.0;
        private const double ZoomStep = 0.1;
        private const double MinZoom = 0.25;
        private const double MaxZoom = 2.0;

        public GraphMetadata Metadata { get; set; } = new();

        public event EventHandler<AgentNode>? NodeSelected;
        public event EventHandler<IReadOnlyCollection<AgentNode>>? SelectionChanged;
        public event EventHandler? SelectionCleared;
        public event EventHandler<NodeConnection>? ConnectionSelected;
        public event EventHandler? GenerateCodeRequested;

        public IReadOnlyList<AgentNode> Nodes => _nodes;
        public IReadOnlyList<NodeConnection> Connections => _connections;

        public AgentCanvas()
        {
            InitializeComponent();
            Focusable = true;
            KeyDown += OnKeyDown;
            Loaded += (s, e) => UpdateGridPattern();
        }

        public void UpdateGridPattern()
        {
            var gridColor = LangraphIDE.ThemeManager.CurrentTheme == LangraphIDE.AppTheme.Dark
                ? Color.FromArgb(40, 255, 255, 255)   // subtle white lines on dark
                : Color.FromArgb(30, 0, 0, 0);        // subtle black lines on light

            var brush = new DrawingBrush
            {
                TileMode = TileMode.Tile,
                Viewport = new Rect(0, 0, 20, 20),
                ViewportUnits = BrushMappingMode.Absolute
            };

            var geometry = new GeometryGroup();
            geometry.Children.Add(new LineGeometry(new Point(20, 0), new Point(20, 20)));
            geometry.Children.Add(new LineGeometry(new Point(0, 20), new Point(20, 20)));

            var drawing = new GeometryDrawing
            {
                Pen = new Pen(new SolidColorBrush(gridColor), 0.5)
            };
            drawing.Geometry = geometry;
            brush.Drawing = drawing;

            GridPattern.Fill = brush;
        }

        #region Node Management

        public AgentNode AddNode(NodeDefinition definition, Point position)
        {
            var node = new AgentNode();
            node.Initialize(definition);

            Canvas.SetLeft(node, position.X);
            Canvas.SetTop(node, position.Y);

            node.NodeSelected += OnNodeSelected;
            node.NodeDragStarted += OnNodeDragStarted;
            node.NodeMoved += OnNodeMoved;
            node.NodeDragging += OnNodeDragging;
            node.ConnectorClicked += OnConnectorClicked;
            node.ConnectorReleased += OnConnectorReleased;
            node.NodeDoubleClicked += OnNodeDoubleClicked;

            NodesCanvas.Children.Add(node);
            _nodes.Add(node);

            _undoStack.Push(new AddNodeAction(this, node));
            _redoStack.Clear();

            return node;
        }

        public void RemoveNode(AgentNode node)
        {
            var connectionsToRemove = _connections
                .Where(c => c.FromNode == node || c.ToNode == node)
                .ToList();

            foreach (var conn in connectionsToRemove)
            {
                RemoveConnection(conn, false);
            }

            NodesCanvas.Children.Remove(node);
            _nodes.Remove(node);
            _selectedNodes.Remove(node);
        }

        private void OnNodeSelected(object? sender, NodeEventArgs e)
        {
            var addToSelection = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);

            if (!addToSelection)
            {
                foreach (var n in _selectedNodes)
                    n.SetSelected(false);
                _selectedNodes.Clear();
                ClearConnectionSelection();
            }

            e.Node.SetSelected(true);
            _selectedNodes.Add(e.Node);

            NodeSelected?.Invoke(this, e.Node);
            SelectionChanged?.Invoke(this, _selectedNodes);
        }

        private void OnNodeDragStarted(object? sender, NodeEventArgs e)
        {
            // Capture start positions for undo
        }

        private void OnNodeMoved(object? sender, NodeEventArgs e)
        {
            UpdateAllConnections();
        }

        private void OnNodeDragging(object? sender, NodeDragEventArgs e)
        {
            // Move all selected nodes together
            if (_selectedNodes.Count > 1 && _selectedNodes.Contains(e.Node))
            {
                foreach (var node in _selectedNodes.Where(n => n != e.Node))
                {
                    var currentX = Canvas.GetLeft(node);
                    var currentY = Canvas.GetTop(node);
                    Canvas.SetLeft(node, currentX + e.Offset.X);
                    Canvas.SetTop(node, currentY + e.Offset.Y);
                }
            }
            UpdateAllConnections();
        }

        private void OnNodeDoubleClicked(object? sender, NodeEventArgs e)
        {
            // Open node properties editor
            NodeSelected?.Invoke(this, e.Node);
        }

        #endregion

        #region Connection Management

        private void OnConnectorClicked(object? sender, ConnectorEventArgs e)
        {
            if (!_isDrawingConnection)
            {
                // Start drawing connection
                _isDrawingConnection = true;
                _connectionStartNode = e.Node;
                _connectionStartConnector = e.ConnectorName;

                _tempConnectionPath = new Path
                {
                    Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#58A6FF")),
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 4, 2 }
                };
                ConnectionsCanvas.Children.Add(_tempConnectionPath);
            }
            else
            {
                // Complete connection on second click (alternative to drag)
                if (_connectionStartNode != null && _connectionStartNode != e.Node)
                {
                    CompleteConnection(_connectionStartNode, _connectionStartConnector!, e.Node, e.ConnectorName);
                }
                CancelConnectionDrawing();
            }
        }

        private void OnConnectorReleased(object? sender, ConnectorEventArgs e)
        {
            // Complete connection on mouse release (drag-to-connect)
            if (_isDrawingConnection && _connectionStartNode != null && _connectionStartNode != e.Node)
            {
                CompleteConnection(_connectionStartNode, _connectionStartConnector!, e.Node, e.ConnectorName);
                CancelConnectionDrawing();
            }
        }

        private void CompleteConnection(AgentNode fromNode, string fromConnector, AgentNode toNode, string toConnector)
        {
            string? condition = null;

            // If connecting from a Router, prompt for condition
            if (fromNode.NodeDefinition?.Type == "Router")
            {
                var dialog = new ConditionInputDialog("continue");
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    condition = dialog.ConditionValue;
                }
                else
                {
                    // User cancelled, don't create connection
                    return;
                }
            }

            CreateConnection(fromNode, fromConnector, toNode, toConnector, condition);
        }

        public NodeConnection? CreateConnection(AgentNode fromNode, string fromConnector, AgentNode toNode, string toConnector, string? condition = null)
        {
            // Check for duplicate connection
            if (_connections.Any(c => c.FromNode == fromNode && c.ToNode == toNode))
                return null;

            var connection = new NodeConnection
            {
                FromNode = fromNode,
                ToNode = toNode,
                FromConnector = fromConnector,
                ToConnector = toConnector,
                Condition = condition,
                IsConditional = !string.IsNullOrEmpty(condition)
            };

            // Create visual path
            connection.PathElement = new Path
            {
                Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString(
                    connection.IsConditional ? "#79C0FF" : "#58A6FF")),
                StrokeThickness = 2
            };

            connection.ArrowElement = new Path
            {
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(
                    connection.IsConditional ? "#79C0FF" : "#58A6FF"))
            };

            connection.HitAreaPath = new Path
            {
                // Use nearly-transparent color instead of fully transparent
                // Fully transparent strokes don't receive hit testing in WPF
                Stroke = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)),
                StrokeThickness = 10,
                Cursor = Cursors.Hand
            };
            connection.HitAreaPath.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ClickCount == 2 && connection.IsConditional)
                {
                    // Double-click to edit condition
                    EditConnectionCondition(connection);
                }
                else
                {
                    SelectConnection(connection);
                }
                e.Handled = true;
            };

            ConnectionsCanvas.Children.Add(connection.PathElement);
            ConnectionsCanvas.Children.Add(connection.ArrowElement);
            // Add hit area to overlay canvas (above nodes) for click detection
            ConnectionHitCanvas.Children.Add(connection.HitAreaPath);

            if (!string.IsNullOrEmpty(condition))
            {
                connection.ConditionLabel = CreateConditionLabel(condition);
                // Make label clickable for selection and double-click for edit
                connection.ConditionLabel.MouseLeftButtonDown += (s, e) =>
                {
                    if (e.ClickCount == 2)
                    {
                        EditConnectionCondition(connection);
                    }
                    else
                    {
                        SelectConnection(connection);
                    }
                    e.Handled = true;
                };
                connection.ConditionLabel.Cursor = Cursors.Hand;
                ConnectionsCanvas.Children.Add(connection.ConditionLabel);
            }

            _connections.Add(connection);
            UpdateConnection(connection);

            _undoStack.Push(new AddConnectionAction(this, connection));
            _redoStack.Clear();

            return connection;
        }

        private Border CreateConditionLabel(string condition)
        {
            return new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#21262D")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#79C0FF")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                Child = new TextBlock
                {
                    Text = condition,
                    FontSize = 10,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#79C0FF"))
                }
            };
        }

        public void RemoveConnection(NodeConnection connection, bool addToUndo = true)
        {
            ConnectionsCanvas.Children.Remove(connection.PathElement);
            ConnectionsCanvas.Children.Remove(connection.ArrowElement);
            ConnectionHitCanvas.Children.Remove(connection.HitAreaPath);
            if (connection.ConditionLabel != null)
                ConnectionsCanvas.Children.Remove(connection.ConditionLabel);

            _connections.Remove(connection);

            if (addToUndo)
            {
                _undoStack.Push(new RemoveConnectionAction(this, connection));
                _redoStack.Clear();
            }
        }

        private void UpdateConnection(NodeConnection connection)
        {
            // Get connector positions relative to ConnectionsCanvas for accurate drawing
            var startPoint = connection.FromNode.GetConnectorPosition(connection.FromConnector, ConnectionsCanvas);
            var endPoint = connection.ToNode.GetConnectorPosition(connection.ToConnector, ConnectionsCanvas);

            // Create bezier curve
            var midX = (startPoint.X + endPoint.X) / 2;
            var geometry = new PathGeometry();
            var figure = new PathFigure { StartPoint = startPoint };

            var bezier = new BezierSegment
            {
                Point1 = new Point(midX, startPoint.Y),
                Point2 = new Point(midX, endPoint.Y),
                Point3 = endPoint
            };
            figure.Segments.Add(bezier);
            geometry.Figures.Add(figure);

            connection.PathElement.Data = geometry;
            connection.HitAreaPath.Data = geometry;

            // Arrow at end
            var arrowSize = 8.0;
            var angle = Math.Atan2(endPoint.Y - bezier.Point2.Y, endPoint.X - bezier.Point2.X);
            var arrowGeometry = new PathGeometry();
            var arrowFigure = new PathFigure { StartPoint = endPoint };
            arrowFigure.Segments.Add(new LineSegment(new Point(
                endPoint.X - arrowSize * Math.Cos(angle - Math.PI / 6),
                endPoint.Y - arrowSize * Math.Sin(angle - Math.PI / 6)), true));
            arrowFigure.Segments.Add(new LineSegment(new Point(
                endPoint.X - arrowSize * Math.Cos(angle + Math.PI / 6),
                endPoint.Y - arrowSize * Math.Sin(angle + Math.PI / 6)), true));
            arrowFigure.IsClosed = true;
            arrowGeometry.Figures.Add(arrowFigure);
            connection.ArrowElement.Data = arrowGeometry;

            // Position condition label
            if (connection.ConditionLabel != null)
            {
                Canvas.SetLeft(connection.ConditionLabel, midX - 30);
                Canvas.SetTop(connection.ConditionLabel, (startPoint.Y + endPoint.Y) / 2 - 10);
            }
        }

        public void UpdateAllConnections()
        {
            foreach (var connection in _connections)
            {
                UpdateConnection(connection);
            }
        }

        private void SelectConnection(NodeConnection connection)
        {
            ClearNodeSelection();
            ClearConnectionSelection();

            connection.IsSelected = true;
            connection.PathElement.Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFA657"));
            connection.PathElement.StrokeThickness = 3;
            connection.ArrowElement.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFA657"));
            _selectedConnection = connection;

            ConnectionSelected?.Invoke(this, connection);
        }

        private void EditConnectionCondition(NodeConnection connection)
        {
            var dialog = new ConditionInputDialog(connection.Condition);
            dialog.Owner = Window.GetWindow(this);

            if (dialog.ShowDialog() == true)
            {
                var newCondition = dialog.ConditionValue;

                // Update the connection
                connection.Condition = newCondition;
                connection.IsConditional = !string.IsNullOrEmpty(newCondition);

                // Update or create label
                if (connection.ConditionLabel != null)
                {
                    // Update existing label
                    if (connection.ConditionLabel.Child is TextBlock textBlock)
                    {
                        textBlock.Text = newCondition;
                    }
                }
                else if (!string.IsNullOrEmpty(newCondition))
                {
                    // Create new label
                    connection.ConditionLabel = CreateConditionLabel(newCondition);
                    connection.ConditionLabel.MouseLeftButtonDown += (s, e) =>
                    {
                        if (e.ClickCount == 2)
                        {
                            EditConnectionCondition(connection);
                        }
                        else
                        {
                            SelectConnection(connection);
                        }
                        e.Handled = true;
                    };
                    connection.ConditionLabel.Cursor = Cursors.Hand;
                    ConnectionsCanvas.Children.Add(connection.ConditionLabel);
                }

                // Update visual
                UpdateConnection(connection);
            }
        }

        private void ClearConnectionSelection()
        {
            if (_selectedConnection != null)
            {
                _selectedConnection.IsSelected = false;
                var color = _selectedConnection.IsConditional ? "#79C0FF" : "#58A6FF";
                _selectedConnection.PathElement.Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
                _selectedConnection.PathElement.StrokeThickness = 2;
                _selectedConnection.ArrowElement.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
                _selectedConnection = null;
            }
        }

        private void ClearNodeSelection()
        {
            foreach (var node in _selectedNodes)
                node.SetSelected(false);
            _selectedNodes.Clear();
        }

        private void CancelConnectionDrawing()
        {
            if (_tempConnectionPath != null)
            {
                ConnectionsCanvas.Children.Remove(_tempConnectionPath);
                _tempConnectionPath = null;
            }
            _isDrawingConnection = false;
            _connectionStartNode = null;
            _connectionStartConnector = null;
        }

        #endregion

        #region Canvas Events

        private void Canvas_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(NodeDefinition)))
            {
                var definition = (NodeDefinition)e.Data.GetData(typeof(NodeDefinition));
                var position = e.GetPosition(NodesCanvas);

                // Snap to grid
                position.X = Math.Round(position.X / 20) * 20;
                position.Y = Math.Round(position.Y / 20) * 20;

                AddNode(definition, position);
            }
        }

        private void Canvas_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(typeof(NodeDefinition))
                ? DragDropEffects.Copy
                : DragDropEffects.None;
            e.Handled = true;
        }

        private void CanvasScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                var delta = e.Delta > 0 ? ZoomStep : -ZoomStep;
                SetZoom(_zoomLevel + delta);
                e.Handled = true;
            }
        }

        private void CanvasScrollViewer_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_isDrawingConnection)
            {
                // Update temp connection line
                return;
            }

            if (e.OriginalSource == GridBackground || e.OriginalSource == GridPattern || e.OriginalSource == NodesCanvas)
            {
                // Start selection rectangle or clear selection
                if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    ClearNodeSelection();
                    ClearConnectionSelection();
                    SelectionCleared?.Invoke(this, EventArgs.Empty);
                }

                _isSelecting = true;
                _selectionStartPoint = e.GetPosition(NodesCanvas);
                SelectionRectangle.Visibility = Visibility.Visible;
                Canvas.SetLeft(SelectionRectangle, _selectionStartPoint.X);
                Canvas.SetTop(SelectionRectangle, _selectionStartPoint.Y);
                SelectionRectangle.Width = 0;
                SelectionRectangle.Height = 0;
            }
        }

        private void CanvasScrollViewer_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isSelecting)
            {
                _isSelecting = false;
                SelectionRectangle.Visibility = Visibility.Collapsed;

                // Select nodes within rectangle
                var selectionRect = new Rect(
                    Canvas.GetLeft(SelectionRectangle),
                    Canvas.GetTop(SelectionRectangle),
                    SelectionRectangle.Width,
                    SelectionRectangle.Height);

                foreach (var node in _nodes)
                {
                    var nodeRect = new Rect(
                        Canvas.GetLeft(node),
                        Canvas.GetTop(node),
                        node.ActualWidth,
                        node.ActualHeight);

                    if (selectionRect.IntersectsWith(nodeRect))
                    {
                        node.SetSelected(true);
                        _selectedNodes.Add(node);
                    }
                }

                if (_selectedNodes.Any())
                    SelectionChanged?.Invoke(this, _selectedNodes);
            }

            // Don't cancel if releasing on a connector - let the connector handle it
            if (e.OriginalSource is Ellipse ellipse && ellipse.Tag is string)
            {
                // Mouse released on a connector, let it handle the connection
                return;
            }

            CancelConnectionDrawing();
        }

        private void CanvasScrollViewer_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isSelecting)
            {
                var currentPoint = e.GetPosition(NodesCanvas);
                var x = Math.Min(currentPoint.X, _selectionStartPoint.X);
                var y = Math.Min(currentPoint.Y, _selectionStartPoint.Y);
                var width = Math.Abs(currentPoint.X - _selectionStartPoint.X);
                var height = Math.Abs(currentPoint.Y - _selectionStartPoint.Y);

                Canvas.SetLeft(SelectionRectangle, x);
                Canvas.SetTop(SelectionRectangle, y);
                SelectionRectangle.Width = width;
                SelectionRectangle.Height = height;
            }

            if (_isDrawingConnection && _tempConnectionPath != null && _connectionStartNode != null)
            {
                var startPoint = _connectionStartNode.GetConnectorPosition(_connectionStartConnector!, ConnectionsCanvas);
                var endPoint = e.GetPosition(ConnectionsCanvas);

                var midX = (startPoint.X + endPoint.X) / 2;
                var geometry = new PathGeometry();
                var figure = new PathFigure { StartPoint = startPoint };
                figure.Segments.Add(new BezierSegment
                {
                    Point1 = new Point(midX, startPoint.Y),
                    Point2 = new Point(midX, endPoint.Y),
                    Point3 = endPoint
                });
                geometry.Figures.Add(figure);
                _tempConnectionPath.Data = geometry;
            }

            if (_isPanning)
            {
                var currentPoint = e.GetPosition(this);
                var offset = currentPoint - _panStartPoint;
                CanvasScrollViewer.ScrollToHorizontalOffset(_scrollStartOffset.X - offset.X);
                CanvasScrollViewer.ScrollToVerticalOffset(_scrollStartOffset.Y - offset.Y);
            }
        }

        private void CanvasScrollViewer_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Could show context menu here
        }

        #endregion

        #region Keyboard

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                DeleteSelected();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                CancelConnectionDrawing();
                ClearNodeSelection();
                ClearConnectionSelection();
                e.Handled = true;
            }
            else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                switch (e.Key)
                {
                    case Key.Z:
                        Undo();
                        e.Handled = true;
                        break;
                    case Key.Y:
                        Redo();
                        e.Handled = true;
                        break;
                    case Key.C:
                        CopySelected();
                        e.Handled = true;
                        break;
                    case Key.V:
                        Paste();
                        e.Handled = true;
                        break;
                    case Key.A:
                        SelectAll();
                        e.Handled = true;
                        break;
                }
            }
        }

        #endregion

        #region Toolbar Actions

        private void BtnUndo_Click(object sender, RoutedEventArgs e) => Undo();
        private void BtnRedo_Click(object sender, RoutedEventArgs e) => Redo();
        private void BtnDelete_Click(object sender, RoutedEventArgs e) => DeleteSelected();
        private void BtnCopy_Click(object sender, RoutedEventArgs e) => CopySelected();
        private void BtnPaste_Click(object sender, RoutedEventArgs e) => Paste();
        private void BtnZoomIn_Click(object sender, RoutedEventArgs e) => SetZoom(_zoomLevel + ZoomStep);
        private void BtnZoomOut_Click(object sender, RoutedEventArgs e) => SetZoom(_zoomLevel - ZoomStep);
        private void BtnZoomReset_Click(object sender, RoutedEventArgs e) => SetZoom(1.0);
        private void BtnFitToView_Click(object sender, RoutedEventArgs e) => FitToView();
        private void BtnGenerateCode_Click(object sender, RoutedEventArgs e) => GenerateCodeRequested?.Invoke(this, EventArgs.Empty);

        #endregion

        #region Actions

        public void DeleteSelected()
        {
            if (_selectedConnection != null)
            {
                RemoveConnection(_selectedConnection);
                _selectedConnection = null;
            }

            var nodesToRemove = _selectedNodes.ToList();
            foreach (var node in nodesToRemove)
            {
                RemoveNode(node);
            }
            _selectedNodes.Clear();
        }

        public void CopySelected()
        {
            if (!_selectedNodes.Any()) return;

            var minX = _selectedNodes.Min(n => Canvas.GetLeft(n));
            var minY = _selectedNodes.Min(n => Canvas.GetTop(n));

            _clipboard = _selectedNodes.Select(n => new ClipboardNodeData
            {
                NodeData = n.ToData(),
                RelativePosition = new Point(Canvas.GetLeft(n) - minX, Canvas.GetTop(n) - minY)
            }).ToList();

            _clipboardConnections = _connections
                .Where(c => _selectedNodes.Contains(c.FromNode) && _selectedNodes.Contains(c.ToNode))
                .Select(c => new ClipboardEdgeData
                {
                    FromNodeId = c.FromNode.NodeId,
                    ToNodeId = c.ToNode.NodeId,
                    FromConnector = c.FromConnector,
                    ToConnector = c.ToConnector,
                    Condition = c.Condition
                }).ToList();
        }

        public void Paste()
        {
            // Would need NodeDefinition lookup - simplified for now
        }

        public void SelectAll()
        {
            foreach (var node in _nodes)
            {
                node.SetSelected(true);
                _selectedNodes.Add(node);
            }
            SelectionChanged?.Invoke(this, _selectedNodes);
        }

        public void Undo()
        {
            if (_undoStack.Count > 0)
            {
                var action = _undoStack.Pop();
                action.Undo();
                _redoStack.Push(action);
            }
        }

        public void Redo()
        {
            if (_redoStack.Count > 0)
            {
                var action = _redoStack.Pop();
                action.Redo();
                _undoStack.Push(action);
            }
        }

        private void SetZoom(double zoom)
        {
            _zoomLevel = Math.Clamp(zoom, MinZoom, MaxZoom);
            ZoomTransform.ScaleX = _zoomLevel;
            ZoomTransform.ScaleY = _zoomLevel;
            ZoomText.Text = $"{(int)(_zoomLevel * 100)}%";
        }

        private void FitToView()
        {
            if (!_nodes.Any()) return;

            var minX = _nodes.Min(n => Canvas.GetLeft(n));
            var minY = _nodes.Min(n => Canvas.GetTop(n));
            var maxX = _nodes.Max(n => Canvas.GetLeft(n) + n.ActualWidth);
            var maxY = _nodes.Max(n => Canvas.GetTop(n) + n.ActualHeight);

            var contentWidth = maxX - minX + 100;
            var contentHeight = maxY - minY + 100;

            var scaleX = CanvasScrollViewer.ViewportWidth / contentWidth;
            var scaleY = CanvasScrollViewer.ViewportHeight / contentHeight;
            var scale = Math.Min(scaleX, scaleY);

            SetZoom(Math.Clamp(scale, MinZoom, MaxZoom));
        }

        public void Clear()
        {
            _connections.Clear();
            _nodes.Clear();
            _selectedNodes.Clear();
            _selectedConnection = null;
            ConnectionsCanvas.Children.Clear();
            ConnectionHitCanvas.Children.Clear();
            NodesCanvas.Children.Clear();
            _undoStack.Clear();
            _redoStack.Clear();
        }

        #endregion

        #region Serialization

        public AgentGraphData ToGraphData()
        {
            return new AgentGraphData
            {
                Metadata = Metadata,
                Nodes = _nodes.Select(n => n.ToData()).ToList(),
                Edges = _connections.Select(c => new AgentEdgeData
                {
                    FromNodeId = c.FromNode.NodeId,
                    ToNodeId = c.ToNode.NodeId,
                    FromConnector = c.FromConnector,
                    ToConnector = c.ToConnector,
                    Condition = c.Condition,
                    Label = c.Label,
                    Priority = c.Priority,
                    IsConditional = c.IsConditional
                }).ToList()
            };
        }

        #endregion
    }

    #region Undo/Redo Actions

    public interface IUndoableAction
    {
        void Undo();
        void Redo();
    }

    public class AddNodeAction : IUndoableAction
    {
        private readonly AgentCanvas _canvas;
        private readonly AgentNode _node;

        public AddNodeAction(AgentCanvas canvas, AgentNode node)
        {
            _canvas = canvas;
            _node = node;
        }

        public void Undo() => _canvas.RemoveNode(_node);
        public void Redo()
        {
            Canvas.SetLeft(_node, _node.ToData().X);
            Canvas.SetTop(_node, _node.ToData().Y);
            // Re-add node
        }
    }

    public class RemoveConnectionAction : IUndoableAction
    {
        private readonly AgentCanvas _canvas;
        private readonly NodeConnection _connection;

        public RemoveConnectionAction(AgentCanvas canvas, NodeConnection connection)
        {
            _canvas = canvas;
            _connection = connection;
        }

        public void Undo()
        {
            _canvas.CreateConnection(
                _connection.FromNode,
                _connection.FromConnector,
                _connection.ToNode,
                _connection.ToConnector,
                _connection.Condition);
        }

        public void Redo() => _canvas.RemoveConnection(_connection, false);
    }

    public class AddConnectionAction : IUndoableAction
    {
        private readonly AgentCanvas _canvas;
        private readonly NodeConnection _connection;

        public AddConnectionAction(AgentCanvas canvas, NodeConnection connection)
        {
            _canvas = canvas;
            _connection = connection;
        }

        public void Undo() => _canvas.RemoveConnection(_connection, false);
        public void Redo()
        {
            _canvas.CreateConnection(
                _connection.FromNode,
                _connection.FromConnector,
                _connection.ToNode,
                _connection.ToConnector,
                _connection.Condition);
        }
    }

    #endregion
}
