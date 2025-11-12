using System.Collections.Concurrent;
using System.Text.Json;
using A2A;
using Microsoft.Extensions.Logging;
using Semantic.Kernel.Agent2AgentProtocol.Example.Core.A2A;
using Semantic.Kernel.Agent2AgentProtocol.Example.Core.Messaging;

namespace Semantic.Kernel.Agent2AgentProtocol.Example.Agent1;

public class Agent1(ILogger<Agent1> logger)
{
    private readonly ILogger<Agent1> _logger = logger;
    private readonly ConcurrentDictionary<IMessagingTransport, TransportState> _transportStates = new();

    public async Task<string?> SendRequestAsync(IMessagingTransport transport, AgentMessage request, CancellationToken cancellationToken = default)
    {
        // Get or create state for this transport
        TransportState state = _transportStates.GetOrAdd(transport, _ => new TransportState(_logger));

        // Initialize transport only once per transport instance
        await state.InitializeAsync(transport, cancellationToken);

        // Send request and wait for response
        return await state.SendAndReceiveAsync(transport, request, cancellationToken);
    }

    private class TransportState
    {
        private readonly ILogger<Agent1> _logger;
        private bool _isInitialized = false;
        private TaskCompletionSource<string?>? _currentResponse;

        public TransportState(ILogger<Agent1> logger)
        {
            _logger = logger;
        }

        public async Task InitializeAsync(IMessagingTransport transport, CancellationToken cancellationToken)
        {
            if(_isInitialized)
                return;

            try
            {
                if(_isInitialized)
                    return;

                _logger.LogInformation("[Agent-1] initializing transport...");

                // Create a non-cancellable token for initialization to prevent premature cancellation
                await transport.StartProcessingAsync(async json =>
                {
                    try
                    {
                        AgentMessage? message = JsonSerializer.Deserialize<AgentMessage>(json, A2AJsonUtilities.DefaultOptions);
                        if(message != null)
                        {
                            (string? text, _, _) = A2AHelper.ParseTaskRequest(message);
                            if(text != null)
                            {
                                _logger.LogInformation("[Agent-1] received response from another agent ← '{Text}'", text);
                                _currentResponse?.TrySetResult(text);
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        _logger.LogError(ex, "[Agent-1] Error processing received message");
                        _currentResponse?.TrySetException(ex);
                    }
                    await Task.CompletedTask;
                }, CancellationToken.None); // Use None to keep the transport alive

                // Give server time to be ready
                try
                {
                    await Task.Delay(1000, cancellationToken);
                }
                catch(OperationCanceledException)
                {
                    // If delay is cancelled, still mark as initialized since transport is ready
                    _logger.LogWarning("[Agent-1] Initialization delay cancelled, but transport is initialized");
                }

                _isInitialized = true;
                _logger.LogInformation("[Agent-1] Transport initialized successfully");
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "[Agent-1] Failed to initialize transport");
                throw;
            }
        }

        public async Task<string?> SendAndReceiveAsync(IMessagingTransport transport, AgentMessage request, CancellationToken cancellationToken)
        {
            try
            {
                _currentResponse = new TaskCompletionSource<string?>();

                _logger.LogInformation("[Agent-1] → sending task with MessageId: {MessageId}", request.MessageId);
                string jsonRequest = JsonSerializer.Serialize(request, A2AJsonUtilities.DefaultOptions);
                await transport.SendMessageAsync(jsonRequest);

                // Wait for response with cancellation support
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                linkedCts.Token.Register(() => _currentResponse.TrySetCanceled(linkedCts.Token));

                string? response = await _currentResponse.Task;
                _logger.LogInformation("[Agent-1] Received response: {Response}", response);
                return response;
            }
            catch(OperationCanceledException)
            {
                _logger.LogWarning("[Agent-1] Request was cancelled");
                throw;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "[Agent-1] Error during send and receive");
                throw;
            }
            finally
            {
                _currentResponse = null;
            }
        }
    }
}