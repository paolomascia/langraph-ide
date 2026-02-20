using System.Collections.Generic;
using Newtonsoft.Json;

namespace LangraphIDE.Models
{
    /// <summary>
    /// Configuration for Langraph code generation
    /// </summary>
    public class LangraphConfig
    {
        [JsonProperty("state_class_name")]
        public string StateClassName { get; set; } = "State";

        [JsonProperty("graph_variable_name")]
        public string GraphVariableName { get; set; } = "graph";

        [JsonProperty("app_variable_name")]
        public string AppVariableName { get; set; } = "app";

        [JsonProperty("include_type_hints")]
        public bool IncludeTypeHints { get; set; } = true;

        [JsonProperty("include_docstrings")]
        public bool IncludeDocstrings { get; set; } = true;

        [JsonProperty("llm_provider")]
        public string LlmProvider { get; set; } = "openai";

        [JsonProperty("llm_model")]
        public string LlmModel { get; set; } = "gpt-4";

        [JsonProperty("additional_imports")]
        public List<string> AdditionalImports { get; set; } = new();
    }

    /// <summary>
    /// Represents a generated Python function for a node
    /// </summary>
    public class GeneratedNodeFunction
    {
        public string NodeId { get; set; } = string.Empty;
        public string FunctionName { get; set; } = string.Empty;
        public string FunctionCode { get; set; } = string.Empty;
        public List<string> RequiredImports { get; set; } = new();
        public bool IsRouter { get; set; }
    }

    /// <summary>
    /// Represents a conditional edge routing configuration
    /// </summary>
    public class ConditionalEdgeConfig
    {
        public string SourceNodeId { get; set; } = string.Empty;
        public string RouterFunctionName { get; set; } = string.Empty;
        public Dictionary<string, string> PathMap { get; set; } = new(); // condition value -> target node
    }

    /// <summary>
    /// Complete generated code structure
    /// </summary>
    public class GeneratedCode
    {
        public List<string> Imports { get; set; } = new();
        public string StateClassCode { get; set; } = string.Empty;
        public List<GeneratedNodeFunction> NodeFunctions { get; set; } = new();
        public string GraphBuildCode { get; set; } = string.Empty;
        public string FullCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// LLM provider configuration templates
    /// </summary>
    public static class LlmProviderTemplates
    {
        public static readonly Dictionary<string, LlmProviderConfig> Providers = new()
        {
            ["openai"] = new LlmProviderConfig
            {
                Name = "OpenAI",
                ClassName = "ChatOpenAI",
                ImportStatement = "from langchain_openai import ChatOpenAI",
                DefaultModel = "gpt-4o",
                UrlParameterName = "base_url",
                ApiKeyParameterName = "api_key",
                ApiKeyConfigVar = "OPEN_API_KEY",
                UrlConfigVar = "OPEN_API_URL",
                ModelConfigVar = "OPEN_API_MODEL"
            },
            ["anthropic"] = new LlmProviderConfig
            {
                Name = "Anthropic",
                ClassName = "ChatAnthropic",
                ImportStatement = "from langchain_anthropic import ChatAnthropic",
                DefaultModel = "claude-sonnet-4-20250514",
                UrlParameterName = "base_url",
                ApiKeyParameterName = "api_key",
                ApiKeyConfigVar = "ANTHROPIC_API_KEY",
                UrlConfigVar = "ANTHROPIC_API_URL",
                ModelConfigVar = "ANTHROPIC_API_MODEL"
            },
            ["gemini"] = new LlmProviderConfig
            {
                Name = "Gemini",
                ClassName = "ChatGoogleGenerativeAI",
                ImportStatement = "from langchain_google_genai import ChatGoogleGenerativeAI",
                DefaultModel = "gemini-2.0-flash",
                UrlParameterName = "transport",
                ApiKeyParameterName = "google_api_key",
                ApiKeyConfigVar = "GEMINI_API_KEY",
                UrlConfigVar = "GEMINI_API_URL",
                ModelConfigVar = "GEMINI_API_MODEL"
            },
            ["mistral"] = new LlmProviderConfig
            {
                Name = "Mistral",
                ClassName = "ChatMistralAI",
                ImportStatement = "from langchain_mistralai import ChatMistralAI",
                DefaultModel = "mistral-large-latest",
                UrlParameterName = "endpoint",
                ApiKeyParameterName = "api_key",
                ApiKeyConfigVar = "MISTRAL_API_KEY",
                UrlConfigVar = "MISTRAL_API_URL",
                ModelConfigVar = "MISTRAL_API_MODEL"
            },
            ["ollama"] = new LlmProviderConfig
            {
                Name = "Ollama",
                ClassName = "ChatOllama",
                ImportStatement = "from langchain_ollama import ChatOllama",
                DefaultModel = "llama3.1",
                UrlParameterName = "base_url",
                ApiKeyParameterName = "api_key",
                ApiKeyConfigVar = "OLLAMA_API_KEY",
                UrlConfigVar = "OLLAMA_API_URL",
                ModelConfigVar = "OLLAMA_API_MODEL"
            },
            ["azure"] = new LlmProviderConfig
            {
                Name = "Azure OpenAI",
                ClassName = "AzureChatOpenAI",
                ImportStatement = "from langchain_openai import AzureChatOpenAI",
                DefaultModel = "gpt-4",
                UrlParameterName = "azure_endpoint",
                ApiKeyParameterName = "api_key",
                ApiKeyConfigVar = "OPEN_API_KEY",
                UrlConfigVar = "OPEN_API_URL",
                ModelConfigVar = "OPEN_API_MODEL"
            }
        };

        /// <summary>
        /// Determine provider key from a node name (e.g. "openai_llm" -> "openai")
        /// </summary>
        public static string DetectProvider(string nodeName)
        {
            var name = nodeName.ToLower();
            if (name.Contains("openai")) return "openai";
            if (name.Contains("anthropic")) return "anthropic";
            if (name.Contains("gemini")) return "gemini";
            if (name.Contains("mistral")) return "mistral";
            if (name.Contains("ollama")) return "ollama";
            if (name.Contains("azure")) return "azure";
            return "openai"; // fallback
        }
    }

    public class LlmProviderConfig
    {
        public string Name { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string ImportStatement { get; set; } = string.Empty;
        public string DefaultModel { get; set; } = string.Empty;
        public string UrlParameterName { get; set; } = "base_url";
        public string ApiKeyParameterName { get; set; } = "api_key";
        public string ApiKeyConfigVar { get; set; } = string.Empty;
        public string UrlConfigVar { get; set; } = string.Empty;
        public string ModelConfigVar { get; set; } = string.Empty;
    }
}
