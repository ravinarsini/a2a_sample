using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Semantic.Kernel.Agent2AgentProtocol.Example.Agent3;
using Semantic.Kernel.Agent2AgentProtocol.Example.Core.Messaging;
using A2A;

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole());

// Configure transport via options
services.Configure<TransportOptions>(cfg =>
{
    cfg.QueueOrPipeName = "a2a-uppercase";
});

services.AddSingleton<IMessagingTransport>(sp =>
{
    TransportOptions options = sp.GetRequiredService<IOptions<TransportOptions>>().Value;
    return new NamedPipeTransport(options.QueueOrPipeName, isServer: true,
        sp.GetRequiredService<ILogger<NamedPipeTransport>>());
});

services.AddSingleton<Kernel>(_ => Kernel.CreateBuilder().Build());
services.AddSingleton<Agent3>();

ServiceProvider provider = services.BuildServiceProvider();

// Create web host for Agent Card endpoint
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5053"); // Unique port for Agent3

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
        Name = "Agent3",
        Description = "Text uppercase conversion agent that converts any input string to uppercase",
        Url = "http://localhost:5053",
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
                Id = "uppercase",
                Name = "uppercase",
                Description = "Converts the input text to uppercase letters",
                Tags = ["text-processing", "string-manipulation", "case-conversion"],
                Examples = ["Input: 'hello world' → Output: 'HELLO WORLD'"]
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
Console.WriteLine("   Agent 3 - Text Uppercase Agent");
Console.WriteLine("═══════════════════════════════════════════════════════════");
Console.WriteLine($"Agent Card URL: http://localhost:5053/.well-known/agent.json");
Console.WriteLine($"Swagger UI: http://localhost:5053/swagger");
Console.WriteLine($"Transport: a2a-uppercase (Named Pipe)");
Console.WriteLine($"Capability: uppercase");
Console.WriteLine("═══════════════════════════════════════════════════════════");
Console.WriteLine();

Agent3 agent = provider.GetRequiredService<Agent3>();
await agent.RunAsync(CancellationToken.None);