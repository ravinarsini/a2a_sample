using System.Collections.Concurrent;

namespace Agent2AgentProtocol.Discovery.Service;

public class InMemoryCapabilityRegistry : ICapabilityRegistry
{
    private readonly ConcurrentDictionary<string, AgentCapability> _capabilities = new();

    public void RegisterCapability(AgentCapability capability)
    {
        _capabilities[capability.Name] = capability;
    }

    public AgentCapability? ResolveCapability(string capabilityName)
    {
        return _capabilities.TryGetValue(capabilityName, out AgentCapability? endpoint) ? endpoint : null;
    }

    public IReadOnlyDictionary<string, AgentCapability> GetAllCapabilities()
    {
        return _capabilities;
    }
}