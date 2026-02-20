using System;
using System.IO;
using LangraphIDE.Models;
using Newtonsoft.Json;

namespace LangraphIDE.Services
{
    public class GraphSerializer
    {
        private static readonly JsonSerializerSettings Settings = new()
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore
        };

        public string Serialize(AgentGraphData graphData)
        {
            graphData.Metadata.ModifiedAt = DateTime.Now;
            return JsonConvert.SerializeObject(graphData, Settings);
        }

        public AgentGraphData? Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<AgentGraphData>(json, Settings);
        }

        public void SaveToFile(AgentGraphData graphData, string filePath)
        {
            var json = Serialize(graphData);
            File.WriteAllText(filePath, json);
        }

        public AgentGraphData? LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            var json = File.ReadAllText(filePath);
            return Deserialize(json);
        }

        public static AgentGraphData CreateEmpty(string name = "New Agent")
        {
            return new AgentGraphData
            {
                Metadata = new GraphMetadata
                {
                    Name = name,
                    CreatedAt = DateTime.Now,
                    ModifiedAt = DateTime.Now
                }
            };
        }
    }
}
