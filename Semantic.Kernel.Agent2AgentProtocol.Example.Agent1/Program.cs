using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using A2A;
using Agent2AgentProtocol.Discovery.Service;
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

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole());

const string DiscoveryUrl = "http://localhost:5000/list";
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
        // This handles newlines within JSON property values
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
            // First attempt failed - could be plain string format or malformed JSON
            // Try to deserialize as plain JSON string (for backward compatibility)
            try
            {
                capability = JsonSerializer.Deserialize<string>(normalizedBody) ?? string.Empty;
            }
            catch(JsonException ex2)
            {
                // Both object and string parsing failed
                // Last resort: try to fix newlines in plain string format and retry
                string fixedJson = normalizedBody;

                // Handle unescaped newlines within the JSON string
                if(fixedJson.StartsWith("\"") && fixedJson.EndsWith("\""))
                {
                    // Remove outer quotes
                    string content = fixedJson.Substring(1, fixedJson.Length - 2);
                    // Replace newlines with spaces
                    content = content.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
                    // Rebuild JSON
                    fixedJson = JsonSerializer.Serialize(content);

                    try
                    {
                        capability = JsonSerializer.Deserialize<string>(fixedJson) ?? string.Empty;
                    }
                    catch(JsonException ex3)
                    {
                        return Results.BadRequest($"Failed to parse request body. Original errors - Object format: {ex1.Message}, String format: {ex2.Message}, Fixed format: {ex3.Message}");
                    }
                }
                else
                {
                    return Results.BadRequest($"Failed to parse request body. Object format error: {ex1.Message}, String format error: {ex2.Message}");
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

    // Step 2: Use LLM to intelligently determine target agent and extract content
    Kernel kernel = serviceProvider.GetRequiredService<Kernel>();
    (string targetSkill, string extractedContent) = await DetermineTargetAgentWithLLM(
   kernel, capability, agentsDict.Values.ToList());

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

                // Build request with extracted content - let the agent router handle it
                string targetAgentName = agent.AgentId ?? GetAgentNameFromSkill(agent.Skill);
                // Use extracted content if available, otherwise use original capability
                string messageText = !string.IsNullOrWhiteSpace(extractedContent) ? extractedContent : capability;
                AgentMessage request = A2AHelper.BuildTaskRequest(messageText, "Agent1", targetAgentName);

                // Create a timeout cancellation token
                string lowerCapability = capability.ToLowerInvariant();
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
        ExtractedContent = extractedContent,
        MatchedCount = matchedAgents.Count,
        Responses = agentResponses
    });
})
.WithName("PostClientCapability")
.WithSummary("Posts a natural language request to matching agents")
.WithDescription(@"Uses LLM to intelligently route requests to agents. Automatically extracts relevant content and determines the best agent.

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
    List<AgentCapability> availableAgents)
{
    try
    {
        // Build list of available agents and their capabilities
        var agentDescriptions = string.Join("\n", availableAgents.Select(a =>
            $"- {a.Skill}: {string.Join(", ", a.Capabilities?.Select(c => c.Description) ?? new[] { a.Name })}"));

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
