using System.Net.Http.Json;
using System.Text.Json;
using A2A;
using Agent2AgentProtocol.Discovery.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Semantic.Kernel.Agent2AgentProtocol.Example.Core.A2A;
using Semantic.Kernel.Agent2AgentProtocol.Example.Core.Messaging;
using Semantic.Kernel.Agent2AgentProtocol.Example.Core.SemanticKernel;

namespace Semantic.Kernel.Agent2AgentProtocol.Example.Agent2;

public class Agent2(
    IMessagingTransport transport,
    Microsoft.SemanticKernel.Kernel kernel,
    IOptions<TransportOptions> options,
    ILogger<Agent2> logger)
{
    private readonly IMessagingTransport _transport = transport;
    private readonly TransportOptions _options = options.Value;
    private readonly ILogger<Agent2> _logger = logger;
    private AgentRouter? _router;

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[Agent-2] waiting for task...");
     
        // Import the TextProcessing plugin
        kernel.ImportPluginFromType<TextProcessingPlugin>("TextProcessing");
 
  // Initialize router after plugin is imported
        _router = new AgentRouter(kernel);

        // Register capabilities with discovery service
        using var client = new HttpClient();

      string json = File.ReadAllText("reverse.card.json");
        AgentCapability? capability = JsonSerializer.Deserialize<AgentCapability>(json);
 if (capability is null)
{
 throw new InvalidOperationException("Failed to deserialize AgentCapability from reverse.card.json.");
    }
   string transportType = "NamedPipe";
        var endpoint = new AgentEndpoint { TransportType = transportType, Address = _options.QueueOrPipeName };
     await client.PostAsJsonAsync("http://localhost:5000/register", new { capability, endpoint }, cancellationToken);
 _logger.LogInformation("[Agent-2] Registered capabilities with discovery service");

        await _transport.StartProcessingAsync(async json =>
 {
       AgentMessage? message = JsonSerializer.Deserialize<AgentMessage>(json, A2AJsonUtilities.DefaultOptions);
         if(message == null)
 return; // not a valid message

(string? text, string? from, string? to) = A2AHelper.ParseTaskRequest(message);
    if(text == null)
        return;  // not a task message
        if(to != "Agent2")
      {
         _logger.LogWarning("[Agent-2] ignored message for {To}", to);
  return;
 }

   _logger.LogInformation("[Agent-2] received: '{Text}' from {From}", text, from);

  string result;
        try
       {
   // Use AgentRouter for intelligent routing
      FunctionResult functionResult = await _router!.RouteAndExecuteAsync(text);
           result = functionResult.ToString();
   _logger.LogInformation("[Agent-2] Successfully processed request using router");
   }
     catch (Exception ex)
     {
_logger.LogError(ex, "[Agent-2] Error processing request with router");
   result = $"[error] {ex.Message}";
       }

  _logger.LogInformation("[Agent-2] → responding with '{Result}'", result);

       AgentMessage response = A2AHelper.BuildTaskRequest(result, "Agent2", from ?? string.Empty);
  string responseJson = JsonSerializer.Serialize(response, A2AJsonUtilities.DefaultOptions);
await _transport.SendMessageAsync(responseJson);
     }, cancellationToken);

     Console.ReadLine();
        await _transport.StopProcessingAsync();
    }
}