using System.Collections.Generic;
using LangraphIDE.Helpers;
using Newtonsoft.Json;

namespace LangraphIDE.Models
{
    /// <summary>
    /// Category of nodes in the toolbox
    /// </summary>
    public class NodeCategory
    {
        [JsonProperty("category")]
        public string Category { get; set; } = string.Empty;

        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        [JsonProperty("icon")]
        public string Icon { get; set; } = "\u2699"; // Gear icon

        [JsonProperty("color")]
        public string Color { get; set; } = "#58A6FF";

        [JsonProperty("nodes")]
        public List<NodeDefinition> Nodes { get; set; } = new();

        public string DisplayName => StringHelpers.ToDisplayName(Category);
    }

    /// <summary>
    /// Definition of a node type available in the toolbox
    /// </summary>
    public class NodeDefinition
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("display_name")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonProperty("type")]
        public string Type { get; set; } = "Custom";

        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        [JsonProperty("icon")]
        public string Icon { get; set; } = "\u2699";

        [JsonProperty("color")]
        public string Color { get; set; } = "#58A6FF";

        [JsonProperty("parameters")]
        public List<NodeParameter> Parameters { get; set; } = new();

        [JsonProperty("code_template")]
        public string CodeTemplate { get; set; } = string.Empty;

        [JsonProperty("output_variables")]
        public List<string> OutputVariables { get; set; } = new();

        [JsonProperty("input_variables")]
        public List<string> InputVariables { get; set; } = new();

        [JsonProperty("has_input_connector")]
        public bool HasInputConnector { get; set; } = true;

        [JsonProperty("has_output_connector")]
        public bool HasOutputConnector { get; set; } = true;

        public string CategoryName { get; set; } = string.Empty;

        public string GetDisplayName() => !string.IsNullOrEmpty(DisplayName)
            ? DisplayName
            : StringHelpers.ToDisplayName(Name);
    }

    /// <summary>
    /// Parameter definition for a node
    /// </summary>
    public class NodeParameter
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("display_name")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonProperty("type")]
        public string Type { get; set; } = "string";

        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        [JsonProperty("required")]
        public bool Required { get; set; }

        [JsonProperty("default")]
        public object? Default { get; set; }

        [JsonProperty("options")]
        public List<string>? Options { get; set; }

        [JsonProperty("multiline")]
        public bool Multiline { get; set; }

        [JsonProperty("input")]
        public ParameterInputConfig? Input { get; set; }

        public string GetDisplayName() => !string.IsNullOrEmpty(DisplayName)
            ? DisplayName
            : StringHelpers.ToDisplayName(Name);
    }

    /// <summary>
    /// Configuration for parameter input control
    /// </summary>
    public class ParameterInputConfig
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "text";

        [JsonProperty("true_value")]
        public string TrueValue { get; set; } = "True";

        [JsonProperty("false_value")]
        public string FalseValue { get; set; } = "False";

        [JsonProperty("min")]
        public double? Min { get; set; }

        [JsonProperty("max")]
        public double? Max { get; set; }
    }
}
