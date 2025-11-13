using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using A2A;
using Agent2AgentProtocol.Discovery.Service;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Semantic.Kernel.Agent2AgentProtocol.Client;
using Semantic.Kernel.Agent2AgentProtocol.Example.Agent1;
using Semantic.Kernel.Agent2AgentProtocol.Example.Core.A2A;
using Semantic.Kernel.Agent2AgentProtocol.Example.Core.Messaging;

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole());

const string DiscoveryUrl = "http://localhost:5000/list";
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5050");

// Add services
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<Agent1>();
// Add a singleton to manage transports
builder.Services.AddSingleton<TransportManager>();

WebApplication app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/api/client/post", async (
    HttpContext context,
    IHttpClientFactory httpClientFactory,
    IServiceProvider serviceProvider,
    [FromBody] string capability) =>
{
    if(string.IsNullOrWhiteSpace(capability))
        return Results.BadRequest("Capability must be provided in the request body.");

    HttpClient client = httpClientFactory.CreateClient();

    // Step 1: Get all agents from discovery service
    Dictionary<string, AgentCapability> agentsDict;
    try
    {
        string json = await client.GetStringAsync(DiscoveryUrl);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        agentsDict = JsonSerializer.Deserialize<Dictionary<string, AgentCapability>>(json, options)
       ?? new Dictionary<string, AgentCapability>();
    }
    catch(Exception ex)
    {
        return Results.Json(new
        {
            Error = "Failed to parse or contact discovery service",
            Details = ex.Message
        });
    }

    if(!agentsDict.Any())
        return Results.NotFound(new { Message = "No agents registered in discovery service." });

    // Step 2: Use intelligent matching - send to all agents and let them decide
    // Or use keyword-based routing for now with improved logic
    string lowerCapability = capability.ToLowerInvariant();
    string targetSkill = DetermineTargetSkill(lowerCapability);

    // Step 3: Find agents matching the determined skill
    var matchedAgents = agentsDict.Values
    .Where(a => string.Equals(a.Skill?.Trim(), targetSkill, StringComparison.OrdinalIgnoreCase))
        .ToList();

    // If no exact match, try to match any agent (they will use router internally)
    if(!matchedAgents.Any())
    {
        matchedAgents = agentsDict.Values.ToList();
    }

    // Step 4: Resolve and invoke agents
    var agentResponses = new List<object>();
    TransportManager transportManager = serviceProvider.GetRequiredService<TransportManager>();

    foreach(AgentCapability? agent in matchedAgents)
    {
        try
        {
            string resolveUrl = $"http://localhost:5000/resolve/{agent.Skill}";
            AgentCapability? endpoint = await client.GetFromJsonAsync<AgentCapability>(resolveUrl);
            if(endpoint != null)
            {
                // Get or create transport for this endpoint
                IMessagingTransport transport = await transportManager.GetOrCreateTransportAsync(
                         endpoint.Address,
                                serviceProvider.GetRequiredService<ILogger<NamedPipeTransport>>());

                Agent1 agent1 = serviceProvider.GetRequiredService<Agent1>();

                // Build request with natural language - let the agent router handle it
                string targetAgentName = agent.AgentId ?? GetAgentNameFromSkill(agent.Skill);
                AgentMessage request = A2AHelper.BuildTaskRequest(capability, "Agent1", targetAgentName);

                // Create a timeout cancellation token (increase for news requests)
                int timeoutSeconds = agent.Skill == "news" || lowerCapability.Contains("news") ? 60 : 30;
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                string? response = await agent1.SendRequestAsync(transport, request, cts.Token);

                agentResponses.Add(new
                {
                    Agent = agent.Skill,
                    Endpoint = endpoint.Address,
                    Response = response ?? "No response received",
                    Success = response != null
                });

                // If we got a successful response, break (don't query all agents)
                if(response != null && !response.Contains("[error]"))
                {
                    break;
                }
            }
        }
        catch(OperationCanceledException)
        {
            agentResponses.Add(new
            {
                Agent = agent.Skill,
                Error = "Request timed out"
            });
        }
        catch(Exception ex)
        {
            agentResponses.Add(new
            {
                Agent = agent.Skill,
                Error = ex.Message
            });
        }
    }

    return Results.Ok(new
    {
        Request = capability,
        DeterminedSkill = targetSkill,
        MatchedCount = matchedAgents.Count,
        Responses = agentResponses
    });
})
.WithName("PostClientCapability")
.WithSummary("Posts a natural language request to matching agents")
.WithDescription("Intelligently routes requests to agents using Semantic Kernel. Agents use internal routers to determine the appropriate action.")
.Produces(200)
.Produces(400)
.Produces(404)
.Produces(502);

ServiceProvider provider = services.BuildServiceProvider();

await app.RunAsync();

// Helper function to determine target skill from natural language
static string DetermineTargetSkill(string request)
{
    if(request.Contains("reverse"))
        return "reverse";
    if(request.Contains("upper") || request.Contains("capitalize"))
        return "uppercase";
    if(request.Contains("news") || request.Contains("search") || request.Contains("find"))
        return "news";

    // Default to first available agent
    return "reverse";
}

// Helper to get agent name from skill
static string GetAgentNameFromSkill(string skill)
{
    return skill?.ToLowerInvariant() switch
    {
        "reverse" => "Agent2",
        "uppercase" => "Agent3",
        "news" => "Agent4",
        _ => "Agent2"
    };
}
