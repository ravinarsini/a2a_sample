namespace Semantic.Kernel.Agent2AgentProtocol.Example.Core.Messaging;

/// <summary>
/// Configuration options for selecting and configuring the messaging transport.
/// </summary>
public class TransportOptions
{
    /// <summary>
    /// Queue name pipe name for named pipes.
    /// </summary>
    public string QueueOrPipeName { get; set; } = string.Empty;
}