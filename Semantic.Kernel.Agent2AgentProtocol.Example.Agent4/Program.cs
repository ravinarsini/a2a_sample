using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Semantic.Kernel.Agent2AgentProtocol.Example.Agent4;
using Semantic.Kernel.Agent2AgentProtocol.Example.Core.Messaging;
using A2A;

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));

// Build configuration
IConfigurationRoot configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

// Configure transport via options
services.Configure<TransportOptions>(cfg =>
{
    cfg.QueueOrPipeName = "a2a-news";
});

services.AddSingleton<IMessagingTransport>(sp =>
{
    TransportOptions options = sp.GetRequiredService<IOptions<TransportOptions>>().Value;
    return new NamedPipeTransport(options.QueueOrPipeName, isServer: true,
        sp.GetRequiredService<ILogger<NamedPipeTransport>>());
});

// Configure Semantic Kernel with OpenAI
string? openAiApiKey = configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
string? openAiModel = configuration["OpenAI:ModelId"] ?? "gpt-4";

if(string.IsNullOrWhiteSpace(openAiApiKey))
{
    Console.WriteLine("WARNING: OpenAI API Key not found!");
    Console.WriteLine("Please set the OPENAI_API_KEY environment variable or add it to appsettings.json");
    Console.WriteLine("Agent will start but news search functionality will not work without a valid API key.");
    Console.WriteLine();
}

services.AddSingleton<Kernel>(sp =>
{
    IKernelBuilder builder = Kernel.CreateBuilder();

    if(!string.IsNullOrWhiteSpace(openAiApiKey))
    {
        builder.AddOpenAIChatCompletion(
                                        modelId: openAiModel,
                                        apiKey: openAiApiKey
                                       );
    }

    return builder.Build();
});

services.AddSingleton<Agent4>();

ServiceProvider provider = services.BuildServiceProvider();

// Create web host for Agent Card endpoint
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5054"); // Unique port for Agent4

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

WebApplication app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

// A2A Standard: Agent Card endpoint
app.MapGet("/.well-known/agent.json", () =>
{
    var agentCard = new AgentCard
    {
        Name = "Agent4",
        Description = "AI-powered news search agent that provides current news summaries on any topic",
        Url = "http://localhost:5054",
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
                Id = "news",
                Name = "search_news",
                Description = "Searches for and summarizes the latest news articles about a given topic using AI",
                Tags = ["news", "search", "ai-powered", "information-retrieval"],
                Examples = [
                    "Input: 'AI' → Output: Latest news about artificial intelligence",
                    "Input: 'climate change' → Output: Recent climate change news",
                    "Input: 'politics in India' → Output: Current Indian political news"
                ]
            }
        ]
    };

    return Results.Ok(agentCard);
})
.WithName("GetAgentCard")
.WithSummary("Get Agent Card (A2A Standard)")
.WithDescription("Returns the Agent Card following the A2A protocol standard at /.well-known/agent.json")
.Produces<AgentCard>(200);

// Alternative endpoint without .well-known
app.MapGet("/agent.json", () =>
{
    return Results.Redirect("/.well-known/agent.json");
})
.WithName("GetAgentCardAlternative")
.ExcludeFromDescription();

// Start the web server in the background
_ = Task.Run(async () => await app.RunAsync());

Console.WriteLine("═══════════════════════════════════════════════════════════");
Console.WriteLine("   Agent 4 - News Search Agent");
Console.WriteLine("═══════════════════════════════════════════════════════════");
Console.WriteLine($"Agent Card URL: http://localhost:5054/.well-known/agent.json");
Console.WriteLine($"Swagger UI: http://localhost:5054/swagger");
Console.WriteLine($"Transport: a2a-news (Named Pipe)");
Console.WriteLine($"Capability: news");
Console.WriteLine($"Registry: Static (no dynamic registration needed)");
Console.WriteLine($"Usage: Send 'news: <topic>' to search for news");
Console.WriteLine("═══════════════════════════════════════════════════════════");
Console.WriteLine();

Agent4 agent = provider.GetRequiredService<Agent4>();
await agent.RunAsync(CancellationToken.None);
