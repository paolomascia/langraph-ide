using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace LangraphIDE.Models
{
    public class AgentInputSettings
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LangraphIDE",
            "agent_inputs.json");

        [JsonProperty("savedInputs")]
        public Dictionary<string, SavedAgentInput> SavedInputs { get; set; } = new();

        [JsonProperty("lastUsedInputName")]
        public string LastUsedInputName { get; set; } = string.Empty;

        public static AgentInputSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    return JsonConvert.DeserializeObject<AgentInputSettings>(json) ?? new AgentInputSettings();
                }
            }
            catch
            {
            }
            return new AgentInputSettings();
        }

        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(SettingsPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(SettingsPath, json);
            }
            catch
            {
            }
        }
    }

    public class SavedAgentInput
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("content")]
        public string Content { get; set; } = string.Empty;

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [JsonProperty("modifiedAt")]
        public DateTime ModifiedAt { get; set; } = DateTime.Now;
    }
}
