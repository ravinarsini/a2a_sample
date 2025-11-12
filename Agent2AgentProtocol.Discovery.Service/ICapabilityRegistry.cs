namespace Agent2AgentProtocol.Discovery.Service;

public interface ICapabilityRegistry
{
    void RegisterCapability(AgentCapability capability);

    AgentCapability? ResolveCapability(string capabilityName);

    IReadOnlyDictionary<string, AgentCapability> GetAllCapabilities();
}