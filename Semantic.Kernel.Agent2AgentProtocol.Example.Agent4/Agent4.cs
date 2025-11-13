using System.Net.Http.Json;
using System.Text.Json;
using A2A;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Semantic.Kernel.Agent2AgentProtocol.Example.Core.A2A;
using Semantic.Kernel.Agent2AgentProtocol.Example.Core.Messaging;
using Semantic.Kernel.Agent2AgentProtocol.Example.Core.SemanticKernel;

namespace Semantic.Kernel.Agent2AgentProtocol.Example.Agent4;

public class Agent4(
    IMessagingTransport transport,
    Microsoft.SemanticKernel.Kernel kernel,
    IOptions<TransportOptions> options,
    ILogger<Agent4> logger)
{
    private readonly IMessagingTransport _transport = transport;
    private readonly TransportOptions _options = options.Value;
    private readonly ILogger<Agent4> _logger = logger;
    private AgentRouter? _router;

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[Agent-4] Starting (registered in static registry)...");

        // Import the TextProcessing plugin
        kernel.ImportPluginFromType<TextProcessingPlugin>("TextProcessing");

        // Initialize router after plugin is imported
        _router = new AgentRouter(kernel);

        // No need to register with discovery service - using static registry
        _logger.LogInformation("[Agent-4] Waiting for tasks on transport: {Transport}", _options.QueueOrPipeName);

        await _transport.StartProcessingAsync(async json =>
        {
            AgentMessage? message = JsonSerializer.Deserialize<AgentMessage>(json, A2AJsonUtilities.DefaultOptions);
            if(message == null)
                return; // not a valid message

            (string? text, string? from, string? to) = A2AHelper.ParseTaskRequest(message);
            if(text == null)
                return;// not a task message
            if(to != "Agent4")
            {
                _logger.LogWarning("[Agent-4] ignored message for {To}", to);
                return;
            }

            _logger.LogInformation("[Agent-4] received: '{Text}' from {From}", text, from);

            string result;
            try
            {
                // Use AgentRouter to intelligently determine the action
                (string functionName, string parameter) = await _router!.DetermineIntentAsync(text);
                
                _logger.LogInformation("[Agent-4] Router determined: Function={Function}, Parameter={Parameter}", 
                functionName, parameter);

                // Execute the function
                KernelPlugin plugin = kernel.Plugins["TextProcessing"];
                KernelFunction function = plugin[functionName];
                KernelArguments arguments = new() { ["input"] = parameter, ["topic"] = parameter };
 
                FunctionResult functionResult = await kernel.InvokeAsync(function, arguments);
                string functionOutput = functionResult.ToString();

                // If it's a news search, create a file
                if (functionName == "search_news")
                {
                    string fileName = $"news_{parameter.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                    string filePath = Path.Combine(Directory.GetCurrentDirectory(), "NewsResults", fileName);

                    // Ensure directory exists
                    Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "NewsResults"));

                    // Write the news content to file
                    await File.WriteAllTextAsync(filePath, functionOutput);

                    result = $"News search completed. File created: {filePath}\n\nContent:\n{functionOutput}";
                    _logger.LogInformation("[Agent-4] Created news file: {FilePath}", filePath);
                }
                else
                {
                    result = functionOutput;
                }
                    
                _logger.LogInformation("[Agent-4] Successfully processed request using router");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Agent-4] Error processing request with router");
                result = $"Error processing request: {ex.Message}";
            }

            _logger.LogInformation("[Agent-4] → responding with result");

            AgentMessage response = A2AHelper.BuildTaskRequest(result, "Agent4", from ?? string.Empty);
            string responseJson = JsonSerializer.Serialize(response, A2AJsonUtilities.DefaultOptions);
            await _transport.SendMessageAsync(responseJson);
        }, cancellationToken);

        Console.ReadLine();
        await _transport.StopProcessingAsync();
    }
}
