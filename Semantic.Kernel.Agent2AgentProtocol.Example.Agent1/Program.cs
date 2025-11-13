using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using A2A;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Semantic.Kernel.Agent2AgentProtocol.Client;
using Semantic.Kernel.Agent2AgentProtocol.Example.Agent1;
using Semantic.Kernel.Agent2AgentProtocol.Example.Core.A2A;
using Semantic.Kernel.Agent2AgentProtocol.Example.Core.Messaging;
using Semantic.Kernel.Agent2AgentProtocol.Example.Core.Registry;

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole());

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5050");

// Add configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

// Add services
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<Agent1>();
builder.Services.AddSingleton<TransportManager>();

// Configure Semantic Kernel for LLM-based routing
var openAiApiKey = configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
var openAiModel = configuration["OpenAI:ModelId"] ?? "gpt-4";

if(!string.IsNullOrWhiteSpace(openAiApiKey))
{
    builder.Services.AddSingleton<Kernel>(sp =>
    {
        var kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.AddOpenAIChatCompletion(
        modelId: openAiModel,
        apiKey: openAiApiKey);
        return kernelBuilder.Build();
    });
}
else
{
    // Fallback to empty kernel if no API key
    builder.Services.AddSingleton<Kernel>(_ => Kernel.CreateBuilder().Build());
    Console.WriteLine("OpenAI API Key not configured. Using fallback keyword-based routing.");
}

WebApplication app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

// Initialize AgentRegistry by loading Agent Cards from endpoints
try
{
    var httpClient = app.Services.GetRequiredService<IHttpClientFactory>().CreateClient();
    await AgentRegistry.InitializeAsync(httpClient);
    Console.WriteLine("AgentRegistry initialized from Agent Card endpoints");
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to initialize AgentRegistry from endpoints: {ex.Message}");
    Console.WriteLine("Using fallback static configuration...");
    AgentRegistry.InitializeFallback();
}

Console.WriteLine("═══════════════════════════════════════════════════════════");
Console.WriteLine("   Agent 1 - Orchestrator Agent");
Console.WriteLine("═══════════════════════════════════════════════════════════");
Console.WriteLine($"Agent Card URL: http://localhost:5050/.well-known/agent.json");
Console.WriteLine($"Swagger UI: http://localhost:5050/swagger");
Console.WriteLine($"API Endpoint: http://localhost:5050/api/client/post");
Console.WriteLine($"Using: Static Agent Registry (loaded from Agent Cards)");
Console.WriteLine($"Available Agents: {string.Join(", ", AgentRegistry.GetAvailableSkills())}");
Console.WriteLine("═══════════════════════════════════════════════════════════");
Console.WriteLine();

// A2A Standard: Agent Card endpoint for Agent1 (Orchestrator)
app.MapGet("/.well-known/agent.json", () =>
{
    var agentCard = new AgentCard
    {
        Name = "Agent1",
        Description = "Intelligent orchestrator agent that routes natural language requests to specialized agents using LLM-based routing",
        Url = "http://localhost:5050",
        Version = "1.0.0",
        Capabilities = new AgentCapabilities
        {
            Streaming = false
        },
        DefaultInputModes = ["text"],
        DefaultOutputModes = ["text"],
        Skills =
        [
            new AgentSkill
            {
                Id = "orchestrate",
                Name = "orchestrate",
                Description = "Intelligently routes requests to appropriate specialized agents (reverse, uppercase, news) using LLM analysis",
                Tags = ["orchestration", "routing", "llm-powered", "multi-agent"],
                Examples = [
                    "Input: 'reverse the string Hello' → Routes to Agent2",
                    "Input: 'make this UPPERCASE: test' → Routes to Agent3",
                    "Input: 'find news about AI' → Routes to Agent4"
                ]
            }
        ]
    };

    return Results.Ok(agentCard);
})
.WithName("GetAgentCard")
.WithSummary("Get Agent Card (A2A Standard)")
.WithDescription("Returns the Agent Card for the orchestrator agent following the A2A protocol standard")
.Produces<AgentCard>(200);

// Alternative endpoint without .well-known
app.MapGet("/agent.json", () =>
{
    return Results.Redirect("/.well-known/agent.json");
})
.WithName("GetAgentCardAlternative")
.ExcludeFromDescription();

// Endpoint to list available agents from static registry
app.MapGet("/api/agents", () =>
{
    var agents = AgentRegistry.GetAllAgents()
        .Select(kvp => new
        {
            Skill = kvp.Value.Skill,
            Name = kvp.Value.Name,
            AgentId = kvp.Value.AgentId,
            Description = kvp.Value.Description,
            Address = kvp.Value.Address,
            Port = kvp.Value.Port,
            AgentCardUrl = kvp.Value.AgentCardUrl,
            Capabilities = kvp.Value.Capabilities.Select(c => new
            {
                c.Name,
                c.Description,
                c.Tags
            })
        });

    return Results.Ok(agents);
})
.WithName("GetAvailableAgents")
.WithSummary("Get all available agents from static registry")
.WithDescription("Returns a list of all agents registered in the static registry (loaded from Agent Card endpoints)")
.Produces(200);

// Endpoint to refresh agent registry from Agent Card endpoints
app.MapPost("/api/agents/refresh", async (IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var httpClient = httpClientFactory.CreateClient();
        await AgentRegistry.RefreshAsync(httpClient);
        
        var agents = AgentRegistry.GetAllAgents();
        return Results.Ok(new
        {
            Message = "Agent registry refreshed successfully",
            AgentCount = agents.Count,
            Agents = agents.Keys
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Failed to refresh agent registry",
            detail: ex.Message,
            statusCode: 500
        );
    }
})
.WithName("RefreshAgentRegistry")
.WithSummary("Refresh agent registry from Agent Card endpoints")
.WithDescription("Reloads all agent information from their Agent Card endpoints (.well-known/agent.json)")
.Produces(200)
.Produces(500);

app.MapPost("/api/client/post", async (
    HttpContext context,
    IHttpClientFactory httpClientFactory,
    IServiceProvider serviceProvider) =>
{
    string capability;

    try
    {
        // Read raw request body to handle multiline input
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
        string rawBody = await reader.ReadToEndAsync();

        if(string.IsNullOrWhiteSpace(rawBody))
        {
            return Results.BadRequest("Request body cannot be empty.");
        }

        // Normalize any multiline content in the raw JSON before parsing
        string normalizedBody = rawBody;

        // Try to deserialize as ClientRequest object first (for Swagger UI)
        try
        {
            var requestModel = JsonSerializer.Deserialize<ClientRequest>(normalizedBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if(requestModel != null && !string.IsNullOrWhiteSpace(requestModel.Prompt))
            {
                capability = requestModel.Prompt;
            }
            else
            {
                return Results.BadRequest("Prompt is required in the request body.");
            }
        }
        catch(JsonException ex1)
        {
            // Try to deserialize as plain JSON string (for backward compatibility)
            try
            {
                capability = JsonSerializer.Deserialize<string>(normalizedBody) ?? string.Empty;
            }
            catch(JsonException ex2)
            {
                // Last resort: try to fix newlines in plain string format
                string fixedJson = normalizedBody;

                if(fixedJson.StartsWith("\"") && fixedJson.EndsWith("\""))
                {
                    string content = fixedJson.Substring(1, fixedJson.Length - 2);
                    content = content.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
                    fixedJson = JsonSerializer.Serialize(content);

                    try
                    {
                        capability = JsonSerializer.Deserialize<string>(fixedJson) ?? string.Empty;
                    }
                    catch(JsonException ex3)
                    {
                        return Results.BadRequest($"Failed to parse request body. Object format: {ex1.Message}, String format: {ex2.Message}, Fixed: {ex3.Message}");
                    }
                }
                else
                {
                    return Results.BadRequest($"Failed to parse request body. Object: {ex1.Message}, String: {ex2.Message}");
                }
            }
        }
    }
    catch(Exception ex)
    {
        return Results.BadRequest($"Failed to read request body: {ex.Message}");
    }

    if(string.IsNullOrWhiteSpace(capability))
        return Results.BadRequest("Capability must be provided in the request body.");

    // Additional normalization for any remaining multiline issues
    capability = capability.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
    capability = System.Text.RegularExpressions.Regex.Replace(capability, @"\s+", " ").Trim();

    // Step 1: Get all agents from static registry (no HTTP call needed!)
    var agents = AgentRegistry.GetAllAgents();
    
    if(!agents.Any())
        return Results.NotFound(new { Message = "No agents available in static registry." });

    // Step 2: Use LLM to intelligently determine target agent and extract content
    Kernel kernel = serviceProvider.GetRequiredService<Kernel>();
    
    // Convert to list for LLM processing
    var agentsList = agents.Select(kvp => new
    {
        Skill = kvp.Value.Skill,
        Name = kvp.Value.Name,
        Description = kvp.Value.Description,
        Capabilities = kvp.Value.Capabilities.Select(c => new
        {
            c.Name,
            c.Description
        }).ToList()
    }).Cast<object>().ToList();
    
    (string targetSkill, string extractedContent) = await DetermineTargetAgentWithLLM(
        kernel, capability, agentsList);

    // Step 3: Resolve agent from static registry
    var registration = AgentRegistry.ResolveAgent(targetSkill);
    
    if(registration == null)
    {
        return Results.NotFound(new 
        { 
            Message = $"No agent found for skill: {targetSkill}",
            AvailableSkills = AgentRegistry.GetAvailableSkills()
        });
    }

    // Step 4: Connect to agent and send request
    var agentResponses = new List<object>();
    TransportManager transportManager = serviceProvider.GetRequiredService<TransportManager>();

    try
    {
        // Get or create transport for this agent
        IMessagingTransport transport = await transportManager.GetOrCreateTransportAsync(
            registration.Address,
            serviceProvider.GetRequiredService<ILogger<NamedPipeTransport>>());

        Agent1 agent1 = serviceProvider.GetRequiredService<Agent1>();

        // Build request with extracted content
        string targetAgentName = registration.AgentId;
        string messageText = !string.IsNullOrWhiteSpace(extractedContent) ? extractedContent : capability;
        AgentMessage request = A2AHelper.BuildTaskRequest(messageText, "Agent1", targetAgentName);

        // Create a timeout cancellation token
        string lowerCapability = capability.ToLowerInvariant();
        int timeoutSeconds = registration.Skill == "news" || lowerCapability.Contains("news") ? 60 : 30;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        
        string? response = await agent1.SendRequestAsync(transport, request, cts.Token);

        agentResponses.Add(new
        {
            Agent = registration.Skill,
            AgentId = registration.AgentId,
            Endpoint = registration.Address,
            Response = response ?? "No response received",
            Success = response != null
        });
    }
    catch(OperationCanceledException)
    {
        agentResponses.Add(new
        {
            Agent = registration.Skill,
            AgentId = registration.AgentId,
            Error = "Request timed out"
        });
    }
    catch(Exception ex)
    {
        agentResponses.Add(new
        {
            Agent = registration.Skill,
            AgentId = registration.AgentId,
            Error = ex.Message
        });
    }

    return Results.Ok(new
    {
        Request = capability,
        DeterminedSkill = targetSkill,
        ExtractedContent = extractedContent,
        TargetAgent = new
        {
            registration.AgentId,
            registration.Name,
            registration.Skill,
            registration.Address
        },
        Responses = agentResponses
    });
})
.WithName("PostClientCapability")
.WithSummary("Posts a natural language request to matching agents")
.WithDescription(@"Uses LLM to intelligently route requests to agents using static registry. Automatically extracts relevant content and determines the best agent.

Send request as JSON object: { ""prompt"": ""your request here"" }
Or as plain JSON string: ""your request here""

Supports multiline input.")
.Accepts<ClientRequest>("application/json")
.Produces(200)
.Produces(400)
.Produces(404)
.Produces(502);

ServiceProvider provider = services.BuildServiceProvider();

await app.RunAsync();

// LLM-based function to determine target agent and extract content
static async Task<(string skill, string content)> DetermineTargetAgentWithLLM(
    Kernel kernel,
    string userRequest,
    List<object> availableAgents)
{
    try
    {
        // Build list of available agents and their capabilities
        var agentDescriptions = string.Join("\n", availableAgents.Select(a =>
        {
            dynamic agent = a;
            var capabilities = agent.Capabilities as IEnumerable<dynamic>;
            var capDesc = capabilities?.Select(c => c.Description?.ToString() ?? "") ?? Enumerable.Empty<string>();
            return $"- {agent.Skill}: {string.Join(", ", capDesc.Where(s => !string.IsNullOrEmpty(s)))}";
        }));

        string prompt = $@"Analyze the following user request and determine which agent should handle it.

User Request: ""{userRequest}""

Available Agents:
{agentDescriptions}

Your task:
1. Determine which agent skill best matches the user's intent
2. Extract the relevant content/parameter that should be sent to that agent

Respond ONLY in this format (no extra text):
SKILL|CONTENT

Examples:
Input: ""reverse the string 'Hello World'"" → reverse|Hello World
Input: ""find news about AI"" → news|AI
Input: ""make this UPPERCASE: test"" → uppercase|test
Input: ""reverse below string Hi Ravi?"" → reverse|Hi Ravi?

Now analyze the user request and respond:";

        FunctionResult result = await kernel.InvokePromptAsync(prompt);
        string response = result.ToString().Trim();

        // Parse response
        string[] parts = response.Split('|', 2);
        if(parts.Length == 2)
        {
            return (parts[0].Trim().ToLowerInvariant(), parts[1].Trim());
        }

        // Fallback to keyword-based if LLM response is invalid
        Console.WriteLine($"LLM response invalid: {response}. Using fallback.");
        return FallbackDetermineTargetSkill(userRequest);
    }
    catch(Exception ex)
    {
        Console.WriteLine($"LLM routing failed: {ex.Message}. Using fallback.");
        return FallbackDetermineTargetSkill(userRequest);
    }
}

// Fallback keyword-based routing (used when LLM is unavailable or fails)
static (string skill, string content) FallbackDetermineTargetSkill(string request)
{
    string lowerRequest = request.ToLowerInvariant();

    if(lowerRequest.Contains("reverse"))
        return ("reverse", request);
    if(lowerRequest.Contains("upper") || lowerRequest.Contains("capitalize"))
        return ("uppercase", request);
    if(lowerRequest.Contains("news") || lowerRequest.Contains("search") || lowerRequest.Contains("find"))
        return ("news", request);

    // Default to reverse
    return ("reverse", request);
}
