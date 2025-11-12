using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using A2A;
using Agent2AgentProtocol.Discovery.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Semantic.Kernel.Agent2AgentProtocol.Example.Core.A2A;
using Semantic.Kernel.Agent2AgentProtocol.Example.Core.Messaging;
using Semantic.Kernel.Agent2AgentProtocol.Example.Core.SemanticKernel;

namespace Semantic.Kernel.Agent2AgentProtocol.Example.Agent3;

public class Agent3(
    IMessagingTransport transport,
    Microsoft.SemanticKernel.Kernel kernel,
    IOptions<TransportOptions> options,
    ILogger<Agent3> logger)
{
    private readonly IMessagingTransport _transport = transport;
    private readonly TransportOptions _options = options.Value;
    private readonly ILogger<Agent3> _logger = logger;

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[Agent-3] waiting for task...");

        // Register capabilities with discovery service
        using var client = new HttpClient();
        string json = File.ReadAllText("uppercase.card.json");
        AgentCapability? capability = JsonSerializer.Deserialize<AgentCapability>(json);
        if (capability is null)
        {
            throw new InvalidOperationException("Failed to deserialize AgentCapability from uppercase.card.json.");
        }
        string transportType = "NamedPipe";
        var endpoint = new AgentEndpoint { TransportType = transportType, Address = _options.QueueOrPipeName };
        await client.PostAsJsonAsync("http://localhost:5000/register", new { capability, endpoint }, cancellationToken);
        _logger.LogInformation("[Agent-3] Registered capabilities with discovery service");

        await _transport.StartProcessingAsync(async json =>
        {
            AgentMessage? message = JsonSerializer.Deserialize<AgentMessage>(json, A2AJsonUtilities.DefaultOptions);
            if(message == null)
                return; // not a valid message

            (string? text, string? from, string? to) = A2AHelper.ParseTaskRequest(message);
            if(text == null)
                return;  // not a task message
            if(to != "Agent3")
            {
                _logger.LogWarning("[Agent-3] ignored message for {To}", to);
                return;
            }

            _logger.LogInformation("[Agent-3] received: '{Text}' from {From}", text, from);

            string result;

            if(text.StartsWith("uppercase:", StringComparison.OrdinalIgnoreCase))
            {
                string input = text["uppercase:".Length..].Trim();

                Microsoft.SemanticKernel.KernelFunction func = TextProcessingFunction.GetFunctionByType("UPPER");
                result = (await kernel.InvokeAsync(func, new() { ["input"] = input })).ToString();
            }
            else
            {
                result = text.StartsWith("upper:", StringComparison.OrdinalIgnoreCase)
                    ? text["upper:".Length..].Trim().ToUpperInvariant()
                    : $"[unhandled] {text}";
            }

            _logger.LogInformation("[Agent-3] → responding with '{Result}'", result);

            AgentMessage response = A2AHelper.BuildTaskRequest(result, "Agent3", from ?? string.Empty);
            string responseJson = JsonSerializer.Serialize(response, A2AJsonUtilities.DefaultOptions);
            await _transport.SendMessageAsync(responseJson);
        }, cancellationToken);

        Console.ReadLine();
        await _transport.StopProcessingAsync();
    }
}