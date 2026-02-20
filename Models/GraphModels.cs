using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Controls;
using Newtonsoft.Json;

namespace LangraphIDE.Models
{
    /// <summary>
    /// Represents the complete agent graph with nodes and edges
    /// </summary>
    public class AgentGraphData
    {
        [JsonProperty("metadata")]
        public GraphMetadata Metadata { get; set; } = new();

        [JsonProperty("nodes")]
        public List<AgentNodeData> Nodes { get; set; } = new();

        [JsonProperty("edges")]
        public List<AgentEdgeData> Edges { get; set; } = new();

        [JsonProperty("state_fields")]
        public List<StateFieldData> StateFields { get; set; } = new();
    }

    /// <summary>
    /// Metadata about the graph
    /// </summary>
    public class GraphMetadata
    {
        [JsonProperty("name")]
        public string Name { get; set; } = "Untitled Agent";

        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        [JsonProperty("author")]
        public string Author { get; set; } = string.Empty;

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [JsonProperty("modified_at")]
        public DateTime ModifiedAt { get; set; } = DateTime.Now;

        [JsonProperty("version")]
        public string Version { get; set; } = "1.0";
    }

    /// <summary>
    /// Types of nodes available in the graph
    /// </summary>
    public enum NodeType
    {
        Start,
        End,
        Input,
        Output,
        Prompt,
        LLM,
        Tool,
        Router,
        Condition,
        Memory,
        RAG,
        Custom
    }

    /// <summary>
    /// Serializable data for a node
    /// </summary>
    public class AgentNodeData
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("display_name")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonProperty("type")]
        public NodeType Type { get; set; } = NodeType.Custom;

        [JsonProperty("x")]
        public double X { get; set; }

        [JsonProperty("y")]
        public double Y { get; set; }

        [JsonProperty("parameters")]
        public Dictionary<string, string> Parameters { get; set; } = new();

        [JsonProperty("python_code")]
        public string PythonCode { get; set; } = string.Empty;

        [JsonProperty("prompt_template")]
        public string PromptTemplate { get; set; } = string.Empty;

        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        [JsonProperty("is_entry_point")]
        public bool IsEntryPoint { get; set; }

        [JsonProperty("output_variables")]
        public List<string> OutputVariables { get; set; } = new();

        [JsonProperty("input_variables")]
        public List<string> InputVariables { get; set; } = new();
    }

    /// <summary>
    /// Serializable data for an edge
    /// </summary>
    public class AgentEdgeData
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("from_node_id")]
        public string FromNodeId { get; set; } = string.Empty;

        [JsonProperty("to_node_id")]
        public string ToNodeId { get; set; } = string.Empty;

        [JsonProperty("from_connector")]
        public string FromConnector { get; set; } = "right";

        [JsonProperty("to_connector")]
        public string ToConnector { get; set; } = "left";

        [JsonProperty("condition")]
        public string? Condition { get; set; }

        [JsonProperty("label")]
        public string? Label { get; set; }

        [JsonProperty("priority")]
        public int Priority { get; set; } = 0;

        [JsonProperty("is_conditional")]
        public bool IsConditional { get; set; }
    }

    /// <summary>
    /// State field definition for the Langraph State TypedDict
    /// </summary>
    public class StateFieldData
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("type")]
        public string Type { get; set; } = "str";

        [JsonProperty("default")]
        public string? Default { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Runtime representation of a connection in the canvas
    /// </summary>
    public class NodeConnection
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public Controls.AgentNode FromNode { get; set; } = null!;
        public Controls.AgentNode ToNode { get; set; } = null!;
        public string FromConnector { get; set; } = "right";
        public string ToConnector { get; set; } = "left";
        public string? Condition { get; set; }
        public string? Label { get; set; }
        public int Priority { get; set; }
        public bool IsConditional { get; set; }
        public Path PathElement { get; set; } = null!;
        public Path ArrowElement { get; set; } = null!;
        public Path HitAreaPath { get; set; } = null!;
        public Border? ConditionLabel { get; set; }
        public bool IsSelected { get; set; }
    }

    /// <summary>
    /// Data for clipboard operations
    /// </summary>
    public class ClipboardNodeData
    {
        public AgentNodeData NodeData { get; set; } = null!;
        public Point RelativePosition { get; set; }
    }

    public class ClipboardEdgeData
    {
        public string FromNodeId { get; set; } = string.Empty;
        public string ToNodeId { get; set; } = string.Empty;
        public string FromConnector { get; set; } = "right";
        public string ToConnector { get; set; } = "left";
        public string? Condition { get; set; }
        public string? Label { get; set; }
    }
}
