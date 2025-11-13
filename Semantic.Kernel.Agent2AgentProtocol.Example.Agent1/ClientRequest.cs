namespace Semantic.Kernel.Agent2AgentProtocol.Example.Agent1;

/// <summary>
/// Request model for the client API
/// </summary>
public class ClientRequest
{
    /// <summary>
    /// The user's natural language request or command
    /// </summary>
    /// <example>reverse below string Hi Ravi?</example>
    public string Prompt { get; set; } = string.Empty;
}
