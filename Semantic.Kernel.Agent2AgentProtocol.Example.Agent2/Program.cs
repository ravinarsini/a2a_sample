using A2A;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Semantic.Kernel.Agent2AgentProtocol.Example.Agent2;
using Semantic.Kernel.Agent2AgentProtocol.Example.Core.Messaging;
using Semantic.Kernel.Agent2AgentProtocol.Example.Core.SemanticKernel;
using System.Text.Json;

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole());

// Configure transport via options
services.Configure<TransportOptions>(cfg =>
{
    cfg.QueueOrPipeName = "a2a-reverse";
});

services.AddSingleton<IMessagingTransport>(sp =>
{
    TransportOptions options = sp.GetRequiredService<IOptions<TransportOptions>>().Value;
    return new NamedPipeTransport(options.QueueOrPipeName, isServer: true,
        sp.GetRequiredService<ILogger<NamedPipeTransport>>());
});

services.AddSingleton<Kernel>(_ => Kernel.CreateBuilder().Build());
services.AddSingleton<Agent2>();

ServiceProvider provider = services.BuildServiceProvider();

// Create web host for Agent Card endpoint
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5052"); // Unique port for Agent2

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
        Name = "Agent2",
        Description = "Text reversal agent that reverses any input string",
        Url = "http://localhost:5052",
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
                Id = "reverse",
                Name = "reverse",
                Description = "Reverses the input text character by character",
                Tags = ["text-processing", "string-manipulation"],
                Examples = ["Input: 'hello' → Output: 'olleh'"]
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
Console.WriteLine("   Agent 2 - Text Reversal Agent");
Console.WriteLine("═══════════════════════════════════════════════════════════");
Console.WriteLine($"Agent Card URL: http://localhost:5052/.well-known/agent.json");
Console.WriteLine($"Swagger UI: http://localhost:5052/swagger");
Console.WriteLine($"Transport: a2a-reverse (Named Pipe)");
Console.WriteLine($"Capability: reverse");
Console.WriteLine($"Registry: Static (no dynamic registration needed)");
Console.WriteLine("═══════════════════════════════════════════════════════════");
Console.WriteLine();

Agent2 agent = provider.GetRequiredService<Agent2>();
await agent.RunAsync(CancellationToken.None);