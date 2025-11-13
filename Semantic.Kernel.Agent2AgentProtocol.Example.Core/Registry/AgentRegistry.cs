using A2A;
using Semantic.Kernel.Agent2AgentProtocol.Example.Core.Messaging;

namespace Semantic.Kernel.Agent2AgentProtocol.Example.Core.Registry;

/// <summary>
/// Static registry of available agents, similar to Semantic Kernel's HostAgentFactory pattern.
/// Eliminates need for separate Discovery Service.
/// </summary>
public static class AgentRegistry
{
    private static readonly Dictionary<string, AgentRegistration> _agents = new()
    {
        ["reverse"] = new AgentRegistration
        {
            AgentId = "Agent2",
            Name = "reverse",
            Skill = "reverse",
            Description = "Text reversal agent that reverses any input string",
            TransportType = "NamedPipe",
            Address = "a2a-reverse",
            AgentCardUrl = "http://localhost:5052/.well-known/agent.json",
            Port = 5052,
            Capabilities = new List<AgentCapabilityInfo>
            {
                new AgentCapabilityInfo
                {
                    Name = "reverse",
                    Description = "Reverses the input text character by character",
                    Tags = new[] { "text-processing", "string-manipulation" }
                }
            }
        },
        
        ["uppercase"] = new AgentRegistration
        {
            AgentId = "Agent3",
            Name = "uppercase",
            Skill = "uppercase",
            Description = "Text uppercase conversion agent",
            TransportType = "NamedPipe",
            Address = "a2a-uppercase",
            AgentCardUrl = "http://localhost:5053/.well-known/agent.json",
            Port = 5053,
            Capabilities = new List<AgentCapabilityInfo>
            {
                new AgentCapabilityInfo
                {
                    Name = "uppercase",
                    Description = "Converts the input text to uppercase letters",
                    Tags = new[] { "text-processing", "case-conversion" }
                }
            }
        },
        
        ["news"] = new AgentRegistration
        {
            AgentId = "Agent4",
            Name = "news",
            Skill = "news",
            Description = "AI-powered news search agent",
            TransportType = "NamedPipe",
            Address = "a2a-news",
            AgentCardUrl = "http://localhost:5054/.well-known/agent.json",
            Port = 5054,
            Capabilities = new List<AgentCapabilityInfo>
            {
                new AgentCapabilityInfo
                {
                    Name = "search_news",
                    Description = "Searches for and summarizes news articles using AI",
                    Tags = new[] { "news", "search", "ai-powered" }
                }
            }
        }
    };

    /// <summary>
    /// Get all registered agents
    /// </summary>
    public static IReadOnlyDictionary<string, AgentRegistration> GetAllAgents()
    {
        return _agents;
    }

    /// <summary>
    /// Resolve agent by skill name
    /// </summary>
    public static AgentRegistration? ResolveAgent(string skill)
    {
        return _agents.TryGetValue(skill.ToLowerInvariant(), out var agent) ? agent : null;
    }

    /// <summary>
    /// Get agent by ID
    /// </summary>
    public static AgentRegistration? GetAgentById(string agentId)
    {
        return _agents.Values.FirstOrDefault(a => 
            string.Equals(a.AgentId, agentId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Check if agent exists
    /// </summary>
    public static bool AgentExists(string skill)
    {
        return _agents.ContainsKey(skill.ToLowerInvariant());
    }

    /// <summary>
    /// Get all available skills
    /// </summary>
    public static IEnumerable<string> GetAvailableSkills()
    {
        return _agents.Keys;
    }
}

/// <summary>
/// Agent registration information
/// </summary>
public class AgentRegistration
{
    public string AgentId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Skill { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TransportType { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string AgentCardUrl { get; set; } = string.Empty;
    public int Port { get; set; }
    public List<AgentCapabilityInfo> Capabilities { get; set; } = new();
}

/// <summary>
/// Agent capability information
/// </summary>
public class AgentCapabilityInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string[] Tags { get; set; } = Array.Empty<string>();
}
