using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Semantic.Kernel.Agent2AgentProtocol.Example.Agent4;
using Semantic.Kernel.Agent2AgentProtocol.Example.Core.Messaging;

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

Console.WriteLine("═══════════════════════════════════════════════════════════");
Console.WriteLine("   Agent 4 - News Search Agent");
Console.WriteLine("═══════════════════════════════════════════════════════════");
Console.WriteLine($"Listening on: a2a-news");
Console.WriteLine($"Capability: news");
Console.WriteLine($"Usage: Send 'news: <topic>' to search for news");
Console.WriteLine("═══════════════════════════════════════════════════════════");
Console.WriteLine();

Agent4 agent = provider.GetRequiredService<Agent4>();
await agent.RunAsync(CancellationToken.None);
