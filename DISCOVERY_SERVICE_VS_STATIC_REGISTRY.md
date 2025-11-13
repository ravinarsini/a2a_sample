# Migration Guide: Discovery Service vs Static Registry

## Executive Summary

**Recommendation:** ? **Remove the Discovery Service** and use a **Static Registry** instead.

Your current setup with a fixed number of local agents is a perfect fit for the static registry pattern used in [Semantic Kernel's A2A samples](https://github.com/microsoft/semantic-kernel/blob/main/dotnet/samples/Demos/A2AClientServer/A2AServer/HostAgentFactory.cs).

---

## Comparison

### Current Architecture (With Discovery Service)

```
???????????????????????????????????????????????????????????????
?                     Discovery Service                         ?
?                    (localhost:5000)                          ?
?  - Maintains registry of agents                              ?
?  - POST /register - Agents register on startup               ?
?  - GET /resolve/{skill} - Resolve agent by skill            ?
?  - GET /list - List all registered agents                    ?
???????????????????????????????????????????????????????????????
             ?                                  ?
             ? Register                         ? Query
             ? (HTTP POST)                      ? (HTTP GET)
             ?                                  ?
????????????????????              ????????????????????????????
?   Agent2, 3, 4   ?              ?        Agent1            ?
?   (Startup)      ?              ?    (Orchestrator)        ?
????????????????????              ????????????????????????????

Issues:
? Requires separate service running
? Network latency for lookups
? More complexity
? Registration can fail
? Extra startup dependency
```

###Simplified Architecture (Static Registry)

```
???????????????????????????????????????????????????????????????
?                      AgentRegistry                           ?
?                   (In-Memory, Static)                        ?
?  - Hardcoded list of agents                                 ?
?  - Instant lookups (no network)                             ?
?  - No startup registration needed                            ?
???????????????????????????????????????????????????????????????
                                  ?
                                  ? Query (In-Memory)
                                  ?
                     ????????????????????????????
                     ?        Agent1            ?
                     ?    (Orchestrator)        ?
                     ????????????????????????????

Benefits:
? No separate service needed
? Zero network latency
? Simpler architecture
? No registration failures
? Faster development
```

---

## When to Use Each Approach

### Use Static Registry When:
? **Fixed set of agents** - Agents don't change frequently  
? **Local deployment** - All agents on same machine  
? **Development/Testing** - Rapid iteration needed  
? **Small scale** - < 10 agents  
? **Configuration-driven** - Agent list known at compile/config time  

**?? This matches your scenario perfectly!**

### Use Discovery Service When:
? **Dynamic registration** - Agents join/leave at runtime  
? **Distributed system** - Agents on different machines  
? **Production microservices** - Need service discovery  
? **Load balancing** - Multiple instances of same agent  
? **Multi-tenant** - Different agent sets per tenant  
? **Large scale** - 10+ agents  

---

## Migration Steps

### Step 1: Add Static Registry (Already Done ?)

File created: `Semantic.Kernel.Agent2AgentProtocol.Example.Core\Registry\AgentRegistry.cs`

### Step 2: Update Agent1 to Use Static Registry

Replace Discovery Service calls with static registry:

**Before (Discovery Service):**
```csharp
// Query discovery service (HTTP GET)
string json = await client.GetStringAsync("http://localhost:5000/list");
var agentsDict = JsonSerializer.Deserialize<Dictionary<string, AgentCapability>>(json, options);

// Resolve agent (HTTP GET)
string resolveUrl = $"http://localhost:5000/resolve/{agent.Skill}";
AgentCapability? endpoint = await client.GetFromJsonAsync<AgentCapability>(resolveUrl);
```

**After (Static Registry):**
```csharp
// Query static registry (in-memory)
var agents = AgentRegistry.GetAllAgents();

// Resolve agent (in-memory)
var registration = AgentRegistry.ResolveAgent(skill);
```

### Step 3: Remove Discovery Service Registration from Agents

**Before (Agent2, Agent3, Agent4):**
```csharp
// Register with discovery service
using var client = new HttpClient();
string json = File.ReadAllText("reverse.card.json");
AgentCapability? capability = JsonSerializer.Deserialize<AgentCapability>(json);
await client.PostAsJsonAsync("http://localhost:5000/register", 
    new { capability, endpoint }, cancellationToken);
```

**After:**
```csharp
// No registration needed - agent is already in static registry
_logger.LogInformation("[Agent-2] Starting (registered in static registry)");
```

### Step 4: Update README and Documentation

Remove Discovery Service from architecture diagrams and instructions.

### Step 5: (Optional) Remove Discovery Service Project

Once migration is complete, you can delete:
- `Agent2AgentProtocol.Discovery.Service` project

---

## Detailed Code Changes

### Change 1: Update Agent1/Program.cs

```csharp
using Semantic.Kernel.Agent2AgentProtocol.Example.Core.Registry;

// REMOVE:
// const string DiscoveryUrl = "http://localhost:5000/list";

app.MapPost("/api/client/post", async (...) =>
{
    // REPLACE:
    // HttpClient client = httpClientFactory.CreateClient();
    // string json = await client.GetStringAsync(DiscoveryUrl);
    // var agentsDict = JsonSerializer.Deserialize<Dictionary<string, AgentCapability>>(json, options);

    // WITH:
    var agents = AgentRegistry.GetAllAgents();
    var agentsList = agents.Select(kvp => new 
    {
        Skill = kvp.Value.Skill,
        Address = kvp.Value.Address,
        AgentId = kvp.Value.AgentId,
        Name = kvp.Value.Name,
        Capabilities = kvp.Value.Capabilities.Select(c => new
        {
            Name = c.Name,
            Description = c.Description
        }).ToList()
    }).ToList();

    // Step 2: Use LLM to determine target agent
    Kernel kernel = serviceProvider.GetRequiredService<Kernel>();
    (string targetSkill, string extractedContent) = await DetermineTargetAgentWithLLM(
        kernel, capability, agentsList);

    // Step 3: Resolve agent from static registry
    var registration = AgentRegistry.ResolveAgent(targetSkill);
    if(registration == null)
        return Results.NotFound(new { Message = $"No agent found for skill: {targetSkill}" });

    // Step 4: Connect to agent
    IMessagingTransport transport = await transportManager.GetOrCreateTransportAsync(
        registration.Address,
        serviceProvider.GetRequiredService<ILogger<NamedPipeTransport>>());

    // Continue with request...
});
```

### Change 2: Update Agent2, Agent3, Agent4

Remove discovery service registration:

```csharp
// REMOVE THIS ENTIRE BLOCK:
/*
using var client = new HttpClient();
string json = File.ReadAllText("reverse.card.json");
AgentCapability? capability = JsonSerializer.Deserialize<AgentCapability>(json);
string transportType = "NamedPipe";
var endpoint = new AgentEndpoint { TransportType = transportType, Address = _options.QueueOrPipeName };
await client.PostAsJsonAsync("http://localhost:5000/register", new { capability, endpoint }, cancellationToken);
_logger.LogInformation("[Agent-2] Registered capabilities with discovery service");
*/

// REPLACE WITH:
_logger.LogInformation("[Agent-2] Starting (already registered in static registry)");
```

### Change 3: Update DetermineTargetAgentWithLLM Signature

```csharp
// BEFORE:
static async Task<(string skill, string content)> DetermineTargetAgentWithLLM(
    Kernel kernel,
    string userRequest,
    List<AgentCapability> availableAgents)  // Discovery Service format

// AFTER:
static async Task<(string skill, string content)> DetermineTargetAgentWithLLM(
    Kernel kernel,
    string userRequest,
    List<object> availableAgents)  // Flexible format
```

---

## Benefits of Migration

### Performance
- **Faster lookups:** In-memory vs HTTP calls
- **No network latency:** Direct method calls
- **Reduced overhead:** No serialization/deserialization

### Simplicity
- **Fewer moving parts:** One less service to manage
- **Easier debugging:** No network calls to trace
- **Simpler deployment:** Don't need to start Discovery Service first

### Reliability
- **No network failures:** Eliminates HTTP call failures
- **No registration failures:** No HTTP POST failures
- **Immediate availability:** Agents available instantly

### Development Experience
- **Faster startup:** No need to wait for discovery service
- **Easier testing:** No need to mock HTTP calls
- **Clear configuration:** Agent list in one place

---

## Migration Checklist

- [ ] Add `AgentRegistry.cs` to Core project (? Done)
- [ ] Update Agent1 to use `AgentRegistry` instead of HTTP calls
- [ ] Remove discovery service registration from Agent2
- [ ] Remove discovery service registration from Agent3
- [ ] Remove discovery service registration from Agent4
- [ ] Test all agents with static registry
- [ ] Update README to remove discovery service instructions
- [ ] (Optional) Delete Discovery Service project
- [ ] Update architecture diagrams

---

## Running Without Discovery Service

### Before (5 terminals needed):
```bash
# Terminal 1 - Discovery Service (REQUIRED)
dotnet run --project Agent2AgentProtocol.Discovery.Service

# Terminal 2-5 - Agents
dotnet run --project Agent2
dotnet run --project Agent3
dotnet run --project Agent4
dotnet run --project Agent1
```

### After (4 terminals needed):
```bash
# Terminal 1-4 - Agents only
dotnet run --project Agent2
dotnet run --project Agent3
dotnet run --project Agent4
dotnet run --project Agent1  # Uses static registry
```

---

## Hybrid Approach (Optional)

If you want flexibility, you can support BOTH:

```csharp
public static class AgentDiscovery
{
    public static async Task<Dictionary<string, AgentRegistration>> GetAgentsAsync(
        bool useStaticRegistry = true)
    {
        if (useStaticRegistry)
        {
            // Use static registry
            return AgentRegistry.GetAllAgents().ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value);
        }
        else
        {
            // Use discovery service
            var client = new HttpClient();
            var json = await client.GetStringAsync("http://localhost:5000/list");
            return JsonSerializer.Deserialize<Dictionary<string, AgentRegistration>>(json);
        }
    }
}
```

---

## Conclusion

**Recommendation: Remove the Discovery Service** ?

Your use case (fixed agents, local deployment, development focus) is **perfectly suited** for the static registry pattern. This matches the approach used in Semantic Kernel's official samples and will:

? Simplify your architecture  
? Improve performance  
? Reduce complexity  
? Make development faster  
? Eliminate a point of failure  

The Discovery Service adds unnecessary overhead for your scenario. Save it for when you truly need dynamic service discovery in a distributed production environment.

---

**Next Steps:**
1. Review the `AgentRegistry.cs` I created
2. Decide if you want to proceed with migration
3. I can help update Agent1 and remove registration from other agents

Would you like me to proceed with the migration?
