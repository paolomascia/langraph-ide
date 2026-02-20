using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using LangraphIDE.Helpers;
using LangraphIDE.Models;

namespace LangraphIDE.Controls
{
    public partial class AgentNode : UserControl
    {
        public string NodeId { get; private set; } = Guid.NewGuid().ToString();
        public NodeDefinition NodeDefinition { get; private set; } = null!;
        public Dictionary<string, string> ParameterValues { get; set; } = new();
        public string? CustomPythonCode { get; set; }
        public string? PromptTemplate { get; set; }
        public bool IsSelected { get; private set; }
        public bool IsEntryPoint { get; set; }

        private bool _isDragging;
        private Point _dragStartPoint;
        private Point _originalPosition;

        public event EventHandler<NodeEventArgs>? NodeSelected;
        public event EventHandler<NodeEventArgs>? NodeMoved;
        public event EventHandler<NodeEventArgs>? NodeDragStarted;
        public event EventHandler<NodeDragEventArgs>? NodeDragging;
        public event EventHandler<ConnectorEventArgs>? ConnectorClicked;
        public event EventHandler<ConnectorEventArgs>? ConnectorReleased;
        public event EventHandler<NodeEventArgs>? NodeDoubleClicked;

        private static readonly Dictionary<string, (string Color, string Icon)> TypeStyles = new()
        {
            { "Start", ("#3FB950", "\u25B6") },
            { "End", ("#F85149", "\u25A0") },
            { "Input", ("#58A6FF", "\u2192") },
            { "Output", ("#58A6FF", "\u2190") },
            { "Prompt", ("#A371F7", "\u270E") },
            { "LLM", ("#A371F7", "\u2605") },
            { "Tool", ("#FFA657", "\u2692") },
            { "Router", ("#79C0FF", "\u2442") },
            { "Condition", ("#79C0FF", "?") },
            { "Memory", ("#7EE787", "\u2630") },
            { "RAG", ("#F778BA", "\uD83D\uDD0D") },
            { "Custom", ("#8B949E", "\u2699") }
        };

        private static readonly Dictionary<string, (string PathData, string Color, string Label)> NodeIcons = new()
        {
            { "input_node", (
                "M269,944 C269.001,943.445 268.555,942.999 268,943 C267.445,943.001 267.001,943.445 267,944 L267,951 C267,951.555 267.445,952.001 268,952 L275,952 C275.555,951.999 275.999,951.556 276,951 C276.001,950.444 275.555,949.999 275,950 L270.509,950 L285.293,935.217 C285.684,934.826 285.684,934.192 285.293,933.802 C284.902,933.412 284.269,933.412 283.879,933.802 L269,948.681 L269,944 Z M284,946 L284,957 C284,958.087 283.086,959 282,959 L261.935,959.033 C260.848,959.033 259.967,958.152 259.967,957.065 L260,937 C260,935.913 260.914,935 262,935 L273,935 L273,933 L262,933 C259.827,933 258,935.221 258,937.394 L258,957.065 C258,959.238 259.762,961 261.935,961 L281.606,961 C283.779,961 286,959.173 286,957 L286,946 L284,946 Z",
                "#58A6FF", "Input") },
            { "output_node", (
                "M336,957 C336,958.087 335.087,959 334,959 L313.935,959.033 C312.848,959.033 311.967,958.152 311.967,957.065 L312,937 C312,935.913 312.913,935 314,935 L325,935 L325,933 L314,933 C311.827,933 310,935.221 310,937.394 L310,957.065 C310,959.238 311.762,961 313.935,961 L333.606,961 C335.779,961 338,959.173 338,957 L338,946 L336,946 L336,957 Z M336.979,933 L330,933 C329.433,933.001 329.001,933.459 329,934 C328.999,934.541 329.433,935.001 330,935 L334.395,934.968 L319.308,949.357 C318.908,949.738 318.908,950.355 319.308,950.736 C319.706,951.117 320.354,951.117 320.753,950.736 L335.971,936.222 L336,941 C335.999,941.541 336.433,942.001 337,942 C337.567,941.999 337.999,941.541 338,941 L338,933.975 C338.001,933.434 337.546,932.999 336.979,933 Z",
                "#58A6FF", "Output") },
            { "api_call", (
                "M26,22a3.86,3.86,0,0,0-2,.57l-3.09-3.1a6,6,0,0,0,0-6.94L24,9.43A3.86,3.86,0,0,0,26,10a4,4,0,1,0-4-4,3.86,3.86,0,0,0,.57,2l-3.1,3.09a6,6,0,0,0-6.94,0L9.43,8A3.86,3.86,0,0,0,10,6a4,4,0,1,0-4,4,3.86,3.86,0,0,0,2-.57l3.09,3.1a6,6,0,0,0,0,6.94L8,22.57A3.86,3.86,0,0,0,6,22a4,4,0,1,0,4,4,3.86,3.86,0,0,0-.57-2l3.1-3.09a6,6,0,0,0,6.94,0L22.57,24A3.86,3.86,0,0,0,22,26a4,4,0,1,0,4-4ZM26,4a2,2,0,1,1-2,2A2,2,0,0,1,26,4ZM4,6A2,2,0,1,1,6,8,2,2,0,0,1,4,6ZM6,28a2,2,0,1,1,2-2A2,2,0,0,1,6,28Zm10-8a4,4,0,1,1,4-4A4,4,0,0,1,16,20Zm10,8a2,2,0,1,1,2-2A2,2,0,0,1,26,28Z",
                "#FFA657", "API") },
            { "state_update", (
                "M5.7 9c.4-2 2.2-3.5 4.3-3.5 1.5 0 2.7.7 3.5 1.8l1.7-2C14 3.9 12.1 3 10 3 6.5 3 3.6 5.6 3.1 9H1l3.5 4L8 9H5.7zm9.8-2L12 11h2.3c-.5 2-2.2 3.5-4.3 3.5-1.5 0-2.7-.7-3.5-1.8l-1.7 1.9C6 16.1 7.9 17 10 17c3.5 0 6.4-2.6 6.9-6H19l-3.5-4z",
                "#7EE787", "Update") },
            { "memory_retrieve", (
                "M307,458.01367 a1,1 0 0 0-1,1 a1,1 0 0 0 1,1 h4 a1,1 0 0 0 1,-1 a1,1 0 0 0-1,-1z " +
                "M323,455.01367 a1,1 0 0 0-0.0508,0.006 a1,1 0 0 0-0.11914,0.0137 a1,1 0 0 0-0.10547,0.0254 a1,1 0 0 0-0.10352,0.0352 a1,1 0 0 0-0.10547,0.0508 a1,1 0 0 0-0.0898,0.0566 a1,1 0 0 0-0.0859,0.0684 a1,1 0 0 0-0.0469,0.0371 l-2,2 a1,1 0 0 0 0,1.41406 a1,1 0 0 0 1.41406,0 L322,458.42773 v3.58594 a1,1 0 0 0 1,1 a1,1 0 0 0 1,-1 v-3.58594 l0.29297,0.29297 a1,1 0 0 0 1.41406,0 a1,1 0 0 0 0,-1.41406 l-1.9707,-1.9707 a1,1 0 0 0-0.40625,-0.26563 a1,1 0 0 0-0.002,0 a1,1 0 0 0-0.004,-0.002 a1,1 0 0 0-0.19922,-0.0449 a1,1 0 0 0-0.0273,-0.004 a1,1 0 0 0-0.0977,-0.006z " +
                "M307,450.01367 a1,1 0 0 0-1,1 a1,1 0 0 0 1,1 h4 a1,1 0 0 0 1,-1 a1,1 0 0 0-1,-1z " +
                "M307,442.01367 a1,1 0 0 0-1,1 a1,1 0 0 0 1,1 h4 a1,1 0 0 0 1,-1 a1,1 0 0 0-1,-1z " +
                "M305,438.01367 c-1.6447,0-3,1.3553-3,3 v4 c0,0.76628 0.29675,1.46716 0.77734,2 c-0.48059,0.53284-0.77734,1.23372-0.77734,2 v4 c0,0.76628 0.29675,1.46716 0.77734,2 c-0.48059,0.53284-0.77734,1.23372-0.77734,2 v4 c0,1.6447 1.3553,3 3,3 h13.11133 c1.26351,1.23579 2.98973,2 4.88867,2 c3.85414,0 7,-3.14585 7,-7 c0,-2.78161-1.63913,-5.19487-4,-6.32226 v-3.67774 c0,-0.76628-0.29675,-1.46716-0.77734,-2 c0.48059,-0.53284 0.77734,-1.23372 0.77734,-2 v-4 c0,-1.6447-1.3553,-3-3,-3z " +
                "M305,440.01367 h18 c0.5713,0 1,0.42871 1,1 v4 c0,0.5713-0.4287,1-1,1 h-18 c-0.5713,0-1,-0.4287-1,-1 v-4 c0,-0.57129 0.4287,-1 1,-1z " +
                "M305,448.01367 h18 c0.5713,0 1,0.42871 1,1 v3.07227 c-0.32711,-0.0472-0.66021,-0.0723-1,-0.0723 c-1.89894,0-3.62516,0.76421-4.88867,2 H305 c-0.5713,0-1,-0.4287-1,-1 v-4 c0,-0.57129 0.4287,-1 1,-1z " +
                "M323,454.01367 c2.77327,0 5,2.22674 5,5 c0,2.77327-2.22673,5-5,5 c-1.44074,0-2.73243,-0.602-3.64258,-1.56836 a1,1 0 0 0-0.16797,-0.18554 C318.44728,461.38795 318,460.25556 318,459.01367 c0,-1.25636 0.45901,-2.39958 1.2168,-3.27539 a1,1 0 0 0 0.0645,-0.0762 c0.91337,-1.01394 2.23794,-1.64844 3.71875,-1.64844z " +
                "M305,456.01367 h11.67773 c-0.43469,0.9103-0.67773,1.92747-0.67773,3 c0,1.07253 0.24304,2.0897 0.67773,3 H305 c-0.5713,0-1,-0.4287-1,-1 v-4 c0,-0.57129 0.4287,-1 1,-1z",
                "#7EE787", "Retrieve") },
            { "memory_store", (
                "M355,458.01367 a1,1 0 0 0-1,1 a1,1 0 0 0 1,1 h4 a1,1 0 0 0 1,-1 a1,1 0 0 0-1,-1z " +
                "M371,455.01367 a1,1 0 0 0-1,1 v3.58594 l-0.29297,-0.29297 A1,1 0 0 0 369,459.01367 a1,1 0 0 0-0.70703,0.29297 a1,1 0 0 0 0,1.41406 l2,2 a1,1 0 0 0 0.0469,0.0371 a1,1 0 0 0 0.0859,0.0684 a1,1 0 0 0 0.0898,0.0566 a1,1 0 0 0 0.10547,0.0508 a1,1 0 0 0 0.10352,0.0352 a1,1 0 0 0 0.10547,0.0254 a1,1 0 0 0 0.11914,0.0137 a1,1 0 0 0 0.0508,0.006 a1,1 0 0 0 0.0508,-0.006 a1,1 0 0 0 0.11914,-0.0137 a1,1 0 0 0 0.10547,-0.0254 a1,1 0 0 0 0.10352,-0.0352 a1,1 0 0 0 0.10547,-0.0508 a1,1 0 0 0 0.0898,-0.0566 a1,1 0 0 0 0.0859,-0.0684 a1,1 0 0 0 0.0469,-0.0371 l2,-2 a1,1 0 0 0 0,-1.41406 a1,1 0 0 0-1.41406,0 L372,459.59961 v-3.58594 a1,1 0 0 0-1,-1z " +
                "M355,450.01367 a1,1 0 0 0-1,1 a1,1 0 0 0 1,1 h4 a1,1 0 0 0 1,-1 a1,1 0 0 0-1,-1z " +
                "M355,442.01367 a1,1 0 0 0-1,1 a1,1 0 0 0 1,1 h4 a1,1 0 0 0 1,-1 a1,1 0 0 0-1,-1z " +
                "M353,438.01367 c-1.6447,0-3,1.3553-3,3 v4 c0,0.76628 0.29675,1.46716 0.77734,2 c-0.48059,0.53284-0.77734,1.23372-0.77734,2 v4 c0,0.76628 0.29675,1.46716 0.77734,2 c-0.48059,0.53284-0.77734,1.23372-0.77734,2 v4 c0,1.6447 1.3553,3 3,3 h13.11133 c1.26351,1.23579 2.98973,2 4.88867,2 c3.85414,0 7,-3.14585 7,-7 c0,-2.78161-1.63913,-5.19487-4,-6.32226 v-3.67774 c0,-0.76628-0.29675,-1.46716-0.77734,-2 c0.48059,-0.53284 0.77734,-1.23372 0.77734,-2 v-4 c0,-1.6447-1.3553,-3-3,-3z " +
                "M353,440.01367 h18 c0.5713,0 1,0.42871 1,1 v4 c0,0.5713-0.4287,1-1,1 h-18 c-0.5713,0-1,-0.4287-1,-1 v-4 c0,-0.57129 0.4287,-1 1,-1z " +
                "M353,448.01367 h18 c0.5713,0 1,0.42871 1,1 v3.07227 c-0.32711,-0.0472-0.66021,-0.0723-1,-0.0723 c-1.89894,0-3.62516,0.76421-4.88867,2 H353 c-0.5713,0-1,-0.4287-1,-1 v-4 c0,-0.57129 0.4287,-1 1,-1z " +
                "M371,454.01367 c2.77327,0 5,2.22674 5,5 c0,2.77327-2.22673,5-5,5 c-1.44074,0-2.73243,-0.602-3.64258,-1.56836 a1,1 0 0 0-0.16797,-0.18554 C366.44728,461.38795 366,460.25556 366,459.01367 c0,-1.25636 0.45901,-2.39958 1.2168,-3.27539 a1,1 0 0 0 0.0645,-0.0762 c0.91337,-1.01394 2.23794,-1.64844 3.71875,-1.64844z " +
                "M353,456.01367 h11.67773 c-0.43469,0.9103-0.67773,1.92747-0.67773,3 c0,1.07253 0.24304,2.0897 0.67773,3 H353 c-0.5713,0-1,-0.4287-1,-1 v-4 c0,-0.57129 0.4287,-1 1,-1z",
                "#7EE787", "Store") },
            { "rag_augment", (
                "M56.89,142.22V85.33c0.03-15.7,12.75-28.42,28.44-28.44l56.89,0c15.71,0,28.44-12.74,28.44-28.44C170.67,12.73,157.93,0,142.22,0H85.33C38.19,0.02,0.02,38.19,0,85.33v56.89c0,15.71,12.74,28.44,28.44,28.44S56.89,157.93,56.89,142.22z " +
                "M0,369.78v56.89c0.02,47.14,38.19,85.31,85.33,85.33h56.89c15.71,0,28.44-12.73,28.44-28.44s-12.74-28.44-28.44-28.44H85.33c-15.7-0.03-28.42-12.75-28.44-28.44l0-56.89c0-15.71-12.74-28.44-28.44-28.44S0,354.07,0,369.78z " +
                "M369.78,56.89h56.89c15.7,0.03,28.42,12.75,28.44,28.44v56.89c0,15.71,12.73,28.44,28.44,28.44c15.71,0,28.44-12.74,28.44-28.44V85.33C511.98,38.19,473.81,0.02,426.67,0h-56.89c-15.71,0-28.44,12.73-28.44,28.44C341.33,44.15,354.07,56.89,369.78,56.89z " +
                "M369.78,512h56.89c47.14-0.02,85.31-38.19,85.33-85.33l0-56.89c0-15.71-12.74-28.44-28.44-28.44c-15.71,0-28.44,12.74-28.44,28.44v56.89c-0.03,15.7-12.75,28.42-28.44,28.44h-56.89c-15.71,0-28.44,12.74-28.44,28.44S354.07,512,369.78,512z " +
                "M271.08,294.34l113.78-71.11c13.32-8.33,17.37-25.87,9.05-39.2c-8.33-13.32-25.87-17.37-39.2-9.05L240.92,246.1c-13.32,8.33-17.37,25.87-9.05,39.2C240.21,298.62,257.75,302.67,271.08,294.34 " +
                "M142.22,199.11l-15.08,24.12l100.41,62.76l0,112.23c0,10.34,5.61,19.86,14.66,24.88c9.05,5.01,20.09,4.72,28.86-0.76l113.78-71.11c8.3-5.19,13.37-14.33,13.37-24.12v-128c0-9.79-5.06-18.93-13.37-24.12l-113.78-71.11c-9.23-5.77-20.92-5.77-30.15,0l-113.78,71.11c-8.3,5.19-13.37,14.33-13.37,24.12c0,9.79,5.06,18.93,13.37,24.12L142.22,199.11l15.08,24.12l98.7-61.69l85.33,53.33v96.47l-56.89,35.56v-76.68c0-9.79-5.06-18.93-13.37-24.12L157.3,174.99L142.22,199.11l15.08,24.12L142.22,199.11z " +
                "M113.78,199.11v128c0,9.79,5.06,18.93,13.37,24.12l113.78,71.11c13.32,8.33,30.87,4.28,39.2-9.05c8.33-13.32,4.28-30.87-9.05-39.2l-100.41-62.76l0-112.23c0-15.71-12.74-28.44-28.44-28.44C126.51,170.67,113.78,183.4,113.78,199.11L113.78,199.11z",
                "#F778BA", "RAG Augment") },
            { "python_code", (
                "M49.39,12.2 C47.07,5.01 40.84,2.04 31.7,2.04 C24.67,2.04 18.15,3.86 18.15,10.14 L18.15,16.07 L31.99,16.07 L31.99,18.07 L11.85,18.07 C6.81,18.07 2.04,21.28 2.04,30.04 C2.04,38.8 6.2,42.59 11.85,42.59 L18.15,42.59 L18.15,34.63 C18.15,29.88 22.38,25.59 27.85,25.59 L40.15,25.59 C44.49,25.59 47.85,22.07 47.85,17.59 L47.85,10.14 C47.85,10.14 49.39,12.2 49.39,12.2 Z M27.28,8.78 C25.7,8.78 24.42,7.5 24.42,5.92 C24.42,4.34 25.7,3.06 27.28,3.06 C28.86,3.06 30.14,4.34 30.14,5.92 C30.14,7.5 28.86,8.78 27.28,8.78 Z " +
                "M18.61,47.8 C20.93,54.99 27.16,57.96 36.3,57.96 C43.33,57.96 49.85,56.14 49.85,49.86 L49.85,43.93 L36.01,43.93 L36.01,41.93 L56.15,41.93 C61.19,41.93 65.96,38.72 65.96,29.96 C65.96,21.2 61.8,17.41 56.15,17.41 L49.85,17.41 L49.85,25.37 C49.85,30.12 45.62,34.41 40.15,34.41 L27.85,34.41 C23.51,34.41 20.15,37.93 20.15,42.41 L20.15,49.86 C20.15,49.86 18.61,47.8 18.61,47.8 Z M40.72,51.22 C42.3,51.22 43.58,52.5 43.58,54.08 C43.58,55.66 42.3,56.94 40.72,56.94 C39.14,56.94 37.86,55.66 37.86,54.08 C37.86,52.5 39.14,51.22 40.72,51.22 Z",
                "#FFA657", "Python") },
        };

        private static readonly Dictionary<string, string> LlmProviderColors = new()
        {
            { "openai", "#10A37F" },
            { "anthropic", "#D97757" },
            { "gemini", "#4285F4" },
            { "mistral", "#FF7000" },
            { "ollama", "#0084FF" },
            { "azure", "#0078D4" }
        };

        public AgentNode()
        {
            InitializeComponent();
            MouseEnter += OnMouseEnter;
            MouseLeave += OnMouseLeave;
        }

        public void SetNodeId(string id) => NodeId = id;

        public void Initialize(NodeDefinition definition)
        {
            NodeDefinition = definition;
            NodeNameText.Text = definition.GetDisplayName();
            DescriptionText.Text = definition.Description;
            NodeTypeText.Text = definition.Type;

            var (color, icon) = GetTypeStyle(definition.Type);
            HeaderBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
            NodeIcon.Text = !string.IsNullOrEmpty(definition.Icon) ? definition.Icon : icon;

            // Set default parameter values
            foreach (var param in definition.Parameters)
            {
                if (param.Default != null && !ParameterValues.ContainsKey(param.Name))
                {
                    ParameterValues[param.Name] = param.Default.ToString() ?? string.Empty;
                }
            }

            // Handle special node types
            SetupNodeStyle(definition.Type);
        }

        private void SetupNodeStyle(string nodeType)
        {
            switch (nodeType)
            {
                case "Start":
                    MainBorder.Visibility = Visibility.Collapsed;
                    StartNode.Visibility = Visibility.Visible;
                    Width = 60;
                    MinHeight = 60;
                    Height = 60;
                    LeftConnector.Visibility = Visibility.Collapsed;
                    TopConnector.Visibility = Visibility.Collapsed;
                    BottomConnector.Visibility = Visibility.Collapsed;
                    RightConnector.Opacity = 0.6;
                    break;

                case "End":
                    MainBorder.Visibility = Visibility.Collapsed;
                    EndNode.Visibility = Visibility.Visible;
                    Width = 60;
                    MinHeight = 60;
                    Height = 60;
                    RightConnector.Visibility = Visibility.Collapsed;
                    TopConnector.Visibility = Visibility.Collapsed;
                    BottomConnector.Visibility = Visibility.Collapsed;
                    LeftConnector.Opacity = 0.6;
                    break;

                case "LLM":
                    MainBorder.Visibility = Visibility.Collapsed;
                    LlmNode.Visibility = Visibility.Visible;
                    Width = 80;
                    MinHeight = 90;
                    Height = 90;
                    // Set provider name and color
                    LlmProviderText.Text = NodeDefinition.GetDisplayName();
                    var providerName = NodeDefinition.Name.Replace("_llm", "").ToLower();
                    if (LlmProviderColors.TryGetValue(providerName, out var providerColor))
                    {
                        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(providerColor));
                        BrainPath.Fill = brush;
                        LlmProviderText.Foreground = brush;
                        LlmBorder.BorderBrush = brush;
                    }
                    break;

                case "Prompt":
                    MainBorder.Visibility = Visibility.Collapsed;
                    PromptNode.Visibility = Visibility.Visible;
                    Width = 80;
                    MinHeight = 90;
                    Height = 90;
                    PromptNameText.Text = "Prompt";
                    break;

                case "Condition":
                    MainBorder.Visibility = Visibility.Collapsed;
                    ConditionNode.Visibility = Visibility.Visible;
                    ConditionNameText.Text = NodeDefinition.GetDisplayName();
                    Width = 80;
                    MinHeight = 80;
                    Height = 80;
                    LeftConnector.Opacity = 0.6;
                    RightConnector.Opacity = 0.6;
                    TopConnector.Opacity = 0.6;
                    BottomConnector.Opacity = 0.6;
                    break;

                case "Router":
                    MainBorder.Visibility = Visibility.Collapsed;
                    RouterNode.Visibility = Visibility.Visible;
                    RouterNameText.Text = NodeDefinition.GetDisplayName();
                    Width = 70;
                    MinHeight = 140;
                    Height = 140;
                    // Single left input, hide standard right/top/bottom
                    LeftConnector.Opacity = 0.6;
                    RightConnector.Visibility = Visibility.Collapsed;
                    TopConnector.Visibility = Visibility.Collapsed;
                    BottomConnector.Visibility = Visibility.Collapsed;
                    break;

                default:
                    if (NodeIcons.TryGetValue(NodeDefinition.Name, out var iconInfo))
                    {
                        MainBorder.Visibility = Visibility.Collapsed;
                        IconNode.Visibility = Visibility.Visible;
                        IconPath.Data = Geometry.Parse(iconInfo.PathData);
                        var iconBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(iconInfo.Color));
                        IconPath.Fill = iconBrush;
                        IconNameText.Foreground = iconBrush;
                        IconBorder.BorderBrush = iconBrush;
                        IconNameText.Text = iconInfo.Label;
                        Width = 80;
                        MinHeight = 90;
                        Height = 90;
                    }
                    if (!NodeDefinition.HasInputConnector)
                    {
                        LeftConnector.Visibility = Visibility.Collapsed;
                    }
                    if (!NodeDefinition.HasOutputConnector)
                    {
                        RightConnector.Visibility = Visibility.Collapsed;
                    }
                    break;
            }
        }

        private static (string Color, string Icon) GetTypeStyle(string nodeType)
        {
            return TypeStyles.TryGetValue(nodeType, out var style)
                ? style
                : ("#8B949E", "\u2699");
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            SelectionBorder.Visibility = selected ? Visibility.Visible : Visibility.Collapsed;
        }

        public void ShowError(bool show)
        {
            ErrorIndicator.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            LlmErrorIndicator.Visibility = show && NodeDefinition?.Type == "LLM" ? Visibility.Visible : Visibility.Collapsed;
            PromptErrorIndicator.Visibility = show && NodeDefinition?.Type == "Prompt" ? Visibility.Visible : Visibility.Collapsed;
            IconErrorIndicator.Visibility = show && IconNode.Visibility == Visibility.Visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public Point GetConnectorPosition(string connector, UIElement? relativeTo = null)
        {
            var element = connector switch
            {
                "left" => LeftConnector,
                "right" => RightConnector,
                "top" => TopConnector,
                "bottom" => BottomConnector,
                "right1" => RouterOut1,
                "right2" => RouterOut2,
                "right3" => RouterOut3,
                "right4" => RouterOut4,
                "right5" => RouterOut5,
                _ => RightConnector
            };

            // If a target element is provided, translate directly to it
            if (relativeTo != null)
            {
                return element.TranslatePoint(new Point(6, 6), relativeTo);
            }

            // Fallback: walk up to find the root canvas for coordinate translation
            DependencyObject? current = this;
            UIElement? rootCanvas = null;
            while (current != null)
            {
                var parent = VisualTreeHelper.GetParent(current);
                if (parent is Canvas canvas)
                {
                    rootCanvas = canvas;
                }
                current = parent;
            }

            if (rootCanvas != null)
            {
                return element.TranslatePoint(new Point(6, 6), rootCanvas);
            }

            // Last fallback: return position relative to node itself
            return element.TranslatePoint(new Point(6, 6), this);
        }

        private void OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (LeftConnector.Visibility == Visibility.Visible)
                LeftConnector.Opacity = 1;
            if (RightConnector.Visibility == Visibility.Visible)
                RightConnector.Opacity = 1;
            if (TopConnector.Visibility == Visibility.Visible)
                TopConnector.Opacity = 1;
            if (BottomConnector.Visibility == Visibility.Visible)
                BottomConnector.Opacity = 1;
            if (NodeDefinition?.Type == "Router")
            {
                RouterOut1.Opacity = 1;
                RouterOut2.Opacity = 1;
                RouterOut3.Opacity = 1;
                RouterOut4.Opacity = 1;
                RouterOut5.Opacity = 1;
            }
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (!IsSelected)
            {
                // Keep some opacity for special nodes
                var baseOpacity = (NodeDefinition?.Type is "Start" or "End" or "Router" or "Condition") ? 0.6 : 0;
                if (LeftConnector.Visibility == Visibility.Visible)
                    LeftConnector.Opacity = baseOpacity;
                if (RightConnector.Visibility == Visibility.Visible)
                    RightConnector.Opacity = baseOpacity;
                if (TopConnector.Visibility == Visibility.Visible)
                    TopConnector.Opacity = baseOpacity;
                if (BottomConnector.Visibility == Visibility.Visible)
                    BottomConnector.Opacity = baseOpacity;
                if (NodeDefinition?.Type == "Router")
                {
                    RouterOut1.Opacity = 0.6;
                    RouterOut2.Opacity = 0.6;
                    RouterOut3.Opacity = 0.6;
                    RouterOut4.Opacity = 0.6;
                    RouterOut5.Opacity = 0.6;
                }
            }
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                NodeDoubleClicked?.Invoke(this, new NodeEventArgs(this));
                e.Handled = true;
                return;
            }

            _isDragging = true;
            _dragStartPoint = e.GetPosition(Parent as UIElement);
            _originalPosition = new Point(Canvas.GetLeft(this), Canvas.GetTop(this));

            if (double.IsNaN(_originalPosition.X)) _originalPosition.X = 0;
            if (double.IsNaN(_originalPosition.Y)) _originalPosition.Y = 0;

            CaptureMouse();
            NodeDragStarted?.Invoke(this, new NodeEventArgs(this));
            NodeSelected?.Invoke(this, new NodeEventArgs(this));
            e.Handled = true;
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                ReleaseMouseCapture();
                NodeMoved?.Invoke(this, new NodeEventArgs(this));
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                var currentPosition = e.GetPosition(Parent as UIElement);
                var offset = currentPosition - _dragStartPoint;

                var newX = _originalPosition.X + offset.X;
                var newY = _originalPosition.Y + offset.Y;

                // Snap to grid (20px)
                newX = Math.Round(newX / 20) * 20;
                newY = Math.Round(newY / 20) * 20;

                // Keep within canvas bounds
                newX = Math.Max(0, newX);
                newY = Math.Max(0, newY);

                Canvas.SetLeft(this, newX);
                Canvas.SetTop(this, newY);

                NodeDragging?.Invoke(this, new NodeDragEventArgs(this, offset));
            }
        }

        private void Connector_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Ellipse connector)
            {
                connector.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#58A6FF"));
            }
        }

        private void Connector_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Ellipse connector)
            {
                connector.Fill = (Brush)FindResource("BorderBrush");
            }
        }

        private void Connector_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Ellipse connector && connector.Tag is string connectorName)
            {
                ConnectorClicked?.Invoke(this, new ConnectorEventArgs(this, connectorName));
                e.Handled = true;
            }
        }

        private void Connector_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Ellipse connector && connector.Tag is string connectorName)
            {
                ConnectorReleased?.Invoke(this, new ConnectorEventArgs(this, connectorName));
                e.Handled = true;
            }
        }

        public AgentNodeData ToData()
        {
            return new AgentNodeData
            {
                Id = NodeId,
                Name = NodeDefinition.Name,
                DisplayName = NodeDefinition.GetDisplayName(),
                Type = Enum.TryParse<NodeType>(NodeDefinition.Type, out var type) ? type : NodeType.Custom,
                X = Canvas.GetLeft(this),
                Y = Canvas.GetTop(this),
                Parameters = new Dictionary<string, string>(ParameterValues),
                PythonCode = CustomPythonCode ?? string.Empty,
                PromptTemplate = PromptTemplate ?? string.Empty,
                Description = NodeDefinition.Description,
                IsEntryPoint = IsEntryPoint
            };
        }
    }

    public class NodeEventArgs : EventArgs
    {
        public AgentNode Node { get; }
        public NodeEventArgs(AgentNode node) => Node = node;
    }

    public class NodeDragEventArgs : NodeEventArgs
    {
        public Vector Offset { get; }
        public NodeDragEventArgs(AgentNode node, Vector offset) : base(node) => Offset = offset;
    }

    public class ConnectorEventArgs : EventArgs
    {
        public AgentNode Node { get; }
        public string ConnectorName { get; }
        public ConnectorEventArgs(AgentNode node, string connectorName)
        {
            Node = node;
            ConnectorName = connectorName;
        }
    }
}
