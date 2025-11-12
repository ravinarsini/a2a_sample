using System.Text.Json.Serialization;

namespace Agent2AgentProtocol.Discovery.Service
{
    public class AgentCapability
    {
        [JsonPropertyName("agentId")]
        public string AgentId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("skill")]
        public string Skill { get; set; }

        [JsonPropertyName("Address")]
        public string Address { get; set; }

        [JsonPropertyName("Endpoint")]
        public string EndpointType { get; set; }

        [JsonPropertyName("capabilities")]
        public List<Capability> Capabilities { get; set; }
    }

    public class Capability
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("input")]
        public Input Input { get; set; }

        [JsonPropertyName("output")]
        public Output Output { get; set; }
    }

    public class Input
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }
    }

    public class Output
    {
        [JsonPropertyName("outputText")]
        public string OutputText { get; set; }
    }
}