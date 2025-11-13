using A2A;
using Semantic.Kernel.Agent2AgentProtocol.Example.Core.Messaging;
using System.Net.Http;
using System.Net.Http.Json;

namespace Semantic.Kernel.Agent2AgentProtocol.Example.Core.Registry;

/// <summary>
/// Static registry of available agents that can be loaded from Agent Card endpoints.
/// Supports both static configuration and dynamic loading from A2A standard endpoints.
/// </summary>
public static class AgentRegistry
{
    private static readonly Dictionary<string, AgentRegistration> _agents = new();
    private static bool _isInitialized = false;
    private static readonly object _lock = new object();

    // Static configuration of agent endpoints
    private static readonly Dictionary<string, AgentEndpointConfig> _agentEndpoints = new()
    {
        ["reverse"] = new AgentEndpointConfig
        {
            AgentId = "Agent2",
            Skill = "reverse",
            Port = 5052,
            Address = "a2a-reverse",
            AgentCardUrl = "http://localhost:5052/.well-known/agent.json"
        },
        ["uppercase"] = new AgentEndpointConfig
        {
            AgentId = "Agent3",
            Skill = "uppercase",
            Port = 5053,
            Address = "a2a-uppercase",
            AgentCardUrl = "http://localhost:5053/.well-known/agent.json"
        },
        ["news"] = new AgentEndpointConfig
        {
            AgentId = "Agent4",
            Skill = "news",
            Port = 5054,
            Address = "a2a-news",
            AgentCardUrl = "http://localhost:5054/.well-known/agent.json"
        }
    };

    /// <summary>
    /// Initialize the registry by loading agent cards from endpoints
    /// </summary>
    public static async Task InitializeAsync(HttpClient? httpClient = null)
    {
        if (_isInitialized)
            return;

        // Check again before async operations
        bool shouldInitialize = false;
        lock (_lock)
        {
            if (!_isInitialized)
            {
                shouldInitialize = true;
            }
        }

        if (!shouldInitialize)
            return;

        using var client = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(2) };

        var loadedAgents = new Dictionary<string, AgentRegistration>();

        foreach (var endpoint in _agentEndpoints)
        {
            try
            {
                var agentCard = await client.GetFromJsonAsync<AgentCard>(endpoint.Value.AgentCardUrl);
                
                if (agentCard != null)
                {
                    loadedAgents[endpoint.Key] = CreateRegistrationFromAgentCard(agentCard, endpoint.Value);
                    Console.WriteLine($"Loaded agent card for '{endpoint.Key}' from {endpoint.Value.AgentCardUrl}");
                }
                else
                {
                    // Fallback to static configuration
                    loadedAgents[endpoint.Key] = CreateFallbackRegistration(endpoint.Value);
                    Console.WriteLine($"Using fallback configuration for '{endpoint.Key}' (agent card not available)");
                }
            }
            catch (Exception ex)
            {
                // Fallback to static configuration
                loadedAgents[endpoint.Key] = CreateFallbackRegistration(endpoint.Value);
                Console.WriteLine($"Failed to load agent card for '{endpoint.Key}': {ex.Message}. Using fallback.");
            }
        }

        // Now lock and update the registry
        lock (_lock)
        {
            if (!_isInitialized)
            {
                foreach (var kvp in loadedAgents)
                {
                    _agents[kvp.Key] = kvp.Value;
                }
                _isInitialized = true;
            }
        }
    }

    /// <summary>
    /// Initialize synchronously using fallback configuration only
    /// </summary>
    public static void InitializeFallback()
    {
        if (_isInitialized)
            return;

        lock (_lock)
        {
            if (_isInitialized)
                return;

            foreach (var endpoint in _agentEndpoints)
            {
                _agents[endpoint.Key] = CreateFallbackRegistration(endpoint.Value);
            }

            _isInitialized = true;
            Console.WriteLine("Initialized AgentRegistry with fallback configuration (Agent Cards not loaded)");
        }
    }

    /// <summary>
    /// Create agent registration from Agent Card
    /// </summary>
    private static AgentRegistration CreateRegistrationFromAgentCard(AgentCard agentCard, AgentEndpointConfig endpoint)
    {
        return new AgentRegistration
        {
            AgentId = endpoint.AgentId,
            Name = agentCard.Name ?? endpoint.Skill,
            Skill = endpoint.Skill,
            Description = agentCard.Description ?? $"{endpoint.AgentId} agent",
            TransportType = "NamedPipe",
            Address = endpoint.Address,
            AgentCardUrl = endpoint.AgentCardUrl,
            Port = endpoint.Port,
            Capabilities = agentCard.Skills?.Select(skill => new AgentCapabilityInfo
            {
                Name = skill.Name ?? skill.Id,
                Description = skill.Description ?? string.Empty,
                Tags = skill.Tags?.ToArray() ?? Array.Empty<string>()
            }).ToList() ?? new List<AgentCapabilityInfo>()
        };
    }

    /// <summary>
    /// Create fallback registration when Agent Card is not available
    /// </summary>
    private static AgentRegistration CreateFallbackRegistration(AgentEndpointConfig endpoint)
    {
        return endpoint.Skill switch
        {
            "reverse" => new AgentRegistration
            {
                AgentId = endpoint.AgentId,
                Name = "reverse",
                Skill = "reverse",
                Description = "Text reversal agent that reverses any input string",
                TransportType = "NamedPipe",
                Address = endpoint.Address,
                AgentCardUrl = endpoint.AgentCardUrl,
                Port = endpoint.Port,
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
            "uppercase" => new AgentRegistration
            {
                AgentId = endpoint.AgentId,
                Name = "uppercase",
                Skill = "uppercase",
                Description = "Text uppercase conversion agent",
                TransportType = "NamedPipe",
                Address = endpoint.Address,
                AgentCardUrl = endpoint.AgentCardUrl,
                Port = endpoint.Port,
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
            "news" => new AgentRegistration
            {
                AgentId = endpoint.AgentId,
                Name = "news",
                Skill = "news",
                Description = "AI-powered news search agent",
                TransportType = "NamedPipe",
                Address = endpoint.Address,
                AgentCardUrl = endpoint.AgentCardUrl,
                Port = endpoint.Port,
                Capabilities = new List<AgentCapabilityInfo>
                {
                    new AgentCapabilityInfo
                    {
                        Name = "search_news",
                        Description = "Searches for and summarizes news articles using AI",
                        Tags = new[] { "news", "search", "ai-powered" }
                    }
                }
            },
            _ => new AgentRegistration
            {
                AgentId = endpoint.AgentId,
                Name = endpoint.Skill,
                Skill = endpoint.Skill,
                Description = $"{endpoint.AgentId} agent",
                TransportType = "NamedPipe",
                Address = endpoint.Address,
                AgentCardUrl = endpoint.AgentCardUrl,
                Port = endpoint.Port,
                Capabilities = new List<AgentCapabilityInfo>()
            }
        };
    }

    /// <summary>
    /// Get all registered agents
    /// </summary>
    public static IReadOnlyDictionary<string, AgentRegistration> GetAllAgents()
    {
        EnsureInitialized();
        return _agents;
    }

    /// <summary>
    /// Resolve agent by skill name
    /// </summary>
    public static AgentRegistration? ResolveAgent(string skill)
    {
        EnsureInitialized();
        return _agents.TryGetValue(skill.ToLowerInvariant(), out var agent) ? agent : null;
    }

    /// <summary>
    /// Get agent by ID
    /// </summary>
    public static AgentRegistration? GetAgentById(string agentId)
    {
        EnsureInitialized();
        return _agents.Values.FirstOrDefault(a => 
            string.Equals(a.AgentId, agentId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Check if agent exists
    /// </summary>
    public static bool AgentExists(string skill)
    {
        EnsureInitialized();
        return _agents.ContainsKey(skill.ToLowerInvariant());
    }

    /// <summary>
    /// Get all available skills
    /// </summary>
    public static IEnumerable<string> GetAvailableSkills()
    {
        EnsureInitialized();
        return _agents.Keys;
    }

    /// <summary>
    /// Refresh agent information from Agent Card endpoints
    /// </summary>
    public static async Task RefreshAsync(HttpClient? httpClient = null)
    {
        _isInitialized = false;
        _agents.Clear();
        await InitializeAsync(httpClient);
    }

    /// <summary>
    /// Reset to fallback configuration
    /// </summary>
    public static void Reset()
    {
        lock (_lock)
        {
            _isInitialized = false;
            _agents.Clear();
        }
    }

    /// <summary>
    /// Ensure registry is initialized
    /// </summary>
    private static void EnsureInitialized()
    {
        if (!_isInitialized)
        {
            InitializeFallback();
        }
    }
}

/// <summary>
/// Agent endpoint configuration
/// </summary>
internal class AgentEndpointConfig
{
    public string AgentId { get; set; } = string.Empty;
    public string Skill { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Address { get; set; } = string.Empty;
    public string AgentCardUrl { get; set; } = string.Empty;
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
