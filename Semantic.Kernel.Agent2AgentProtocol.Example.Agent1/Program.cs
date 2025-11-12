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

    // Extract the keyword before the first colon (or the whole string if no colon)
    string keyword = capability.Split(new[] { ':' }, 2)[0].Trim();

    if(string.IsNullOrWhiteSpace(keyword))
    {
        return Results.BadRequest("Unable to extract keyword from capability string.");
    }

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

    // Step 2: Find agents matching the keyword
    var matchedAgents = agentsDict.Values
        .Where(a => string.Equals(a.Skill?.Trim(), keyword, StringComparison.OrdinalIgnoreCase))
        .ToList();

    if(!matchedAgents.Any())
        return Results.NotFound(new { Message = $"No agents found with skill matching '{keyword}'." });

    // Step 3: Resolve each matched agent endpoint dynamically and get responses
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
                AgentMessage request;
                string inputText = capability.Split(new[] { ':' }, 2).Length > 1 ? capability.Split(new[] { ':' }, 2)[1].Trim()
                                              : "";

                if(agent.Skill == "reverse")
                {
                    request = A2AHelper.BuildTaskRequest($"reverse: {inputText}", "Agent1", "Agent2");
                }
                else
                {
                    request = A2AHelper.BuildTaskRequest($"uppercase: {inputText}", "Agent1", "Agent3");
                }

                // Create a timeout cancellation token
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                string? response = await agent1.SendRequestAsync(transport, request, cts.Token);

                agentResponses.Add(new
                {
                    Agent = agent.Skill,
                    Endpoint = endpoint.Address,
                    Response = response ?? "No response received",
                    Success = response != null
                });
            }
        }
        catch(OperationCanceledException)
        {
            agentResponses.Add(new
            {
                Agent = agent.Skill,
                Error = "Request timed out after 30 seconds"
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
        Keyword = keyword,
        MatchedCount = matchedAgents.Count,
        Responses = agentResponses
    });
})
.WithName("PostClientCapability")
.WithSummary("Posts a capability to matching agents")
.WithDescription("Queries the discovery service, finds matching agents by capability, resolves their endpoints, and invokes them to get responses.")
.Produces(200)
.Produces(400)
.Produces(404)
.Produces(502);

ServiceProvider provider = services.BuildServiceProvider();

await app.RunAsync();
