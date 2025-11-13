# A2A Standard Agent Card URLs Implementation

## Overview

All agents now expose their Agent Cards following the **A2A (Agent-to-Agent) Protocol** standard at the `/.well-known/agent.json` endpoint.

## What is an Agent Card?

An Agent Card is a machine-readable description of an agent's capabilities, following the A2A protocol standard. It includes:
- Agent metadata (name, description, version, URL)
- Capabilities (streaming, multi-turn conversations, etc.)
- Skills with detailed descriptions
- Input/output modes
- Examples of usage

## Agent Card Endpoints

| Agent | Port | Agent Card URL | Swagger UI |
|-------|------|----------------|------------|
| **Agent1** (Orchestrator) | 5050 | http://localhost:5050/.well-known/agent.json | http://localhost:5050/swagger |
| **Agent2** (Reverse) | 5052 | http://localhost:5052/.well-known/agent.json | http://localhost:5052/swagger |
| **Agent3** (Uppercase) | 5053 | http://localhost:5053/.well-known/agent.json | http://localhost:5053/swagger |
| **Agent4** (News Search) | 5054 | http://localhost:5054/.well-known/agent.json | http://localhost:5054/swagger |

## Agent Card Examples

### Agent1 - Orchestrator

```json
{
  "name": "Agent1",
  "description": "Intelligent orchestrator agent that routes natural language requests to specialized agents using LLM-based routing",
  "url": "http://localhost:5050",
  "version": "1.0.0",
  "capabilities": {
    "streaming": false
  },
  "defaultInputModes": ["text"],
  "defaultOutputModes": ["text"],
  "skills": [
    {
      "id": "orchestrate",
      "name": "orchestrate",
      "description": "Intelligently routes requests to appropriate specialized agents (reverse, uppercase, news) using LLM analysis",
      "tags": ["orchestration", "routing", "llm-powered", "multi-agent"],
      "examples": [
        "Input: 'reverse the string Hello' ? Routes to Agent2",
        "Input: 'make this UPPERCASE: test' ? Routes to Agent3",
        "Input": 'find news about AI' ? Routes to Agent4"
      ]
    }
  ]
}
```

### Agent2 - Text Reversal

```json
{
  "name": "Agent2",
  "description": "Text reversal agent that reverses any input string",
  "url": "http://localhost:5052",
  "version": "1.0.0",
  "capabilities": {
    "streaming": false
  },
  "defaultInputModes": ["text"],
  "defaultOutputModes": ["text"],
  "skills": [
    {
      "id": "reverse",
      "name": "reverse",
      "description": "Reverses the input text character by character",
      "tags": ["text-processing", "string-manipulation"],
      "examples": ["Input: 'hello' ? Output: 'olleh'"]
    }
  ]
}
```

### Agent3 - Uppercase Conversion

```json
{
  "name": "Agent3",
  "description": "Text uppercase conversion agent that converts any input string to uppercase",
  "url": "http://localhost:5053",
  "version": "1.0.0",
  "capabilities": {
    "streaming": false
  },
  "defaultInputModes": ["text"],
  "defaultOutputModes": ["text"],
  "skills": [
    {
      "id": "uppercase",
      "name": "uppercase",
      "description": "Converts the input text to uppercase letters",
      "tags": ["text-processing", "string-manipulation", "case-conversion"],
      "examples": ["Input: 'hello world' ? Output: 'HELLO WORLD'"]
    }
  ]
}
```

### Agent4 - News Search

```json
{
  "name": "Agent4",
  "description": "AI-powered news search agent that provides current news summaries on any topic",
  "url": "http://localhost:5054",
  "version": "1.0.0",
  "capabilities": {
    "streaming": false
  },
  "defaultInputModes": ["text"],
  "defaultOutputModes": ["text"],
  "skills": [
    {
      "id": "news",
      "name": "search_news",
      "description": "Searches for and summarizes the latest news articles about a given topic using AI",
      "tags": ["news", "search", "ai-powered", "information-retrieval"],
      "examples": [
        "Input: 'AI' ? Output: Latest news about artificial intelligence",
        "Input: 'climate change' ? Output: Recent climate change news",
        "Input: 'politics in India' ? Output: Current Indian political news"
      ]
    }
  ]
}
```

## Testing Agent Cards

### Using curl

```bash
# Test Agent1 (Orchestrator)
curl http://localhost:5050/.well-known/agent.json

# Test Agent2 (Reverse)
curl http://localhost:5052/.well-known/agent.json

# Test Agent3 (Uppercase)
curl http://localhost:5053/.well-known/agent.json

# Test Agent4 (News)
curl http://localhost:5054/.well-known/agent.json
```

### Using Browser

Simply navigate to any of the Agent Card URLs:
- http://localhost:5050/.well-known/agent.json
- http://localhost:5052/.well-known/agent.json
- http://localhost:5053/.well-known/agent.json
- http://localhost:5054/.well-known/agent.json

### Using PowerShell

```powershell
# Get all agent cards
$agents = @(5050, 5052, 5053, 5054)
foreach ($port in $agents) {
    Write-Host "Agent Card for port $port:" -ForegroundColor Cyan
    Invoke-RestMethod -Uri "http://localhost:$port/.well-known/agent.json" | ConvertTo-Json -Depth 5
    Write-Host ""
}
```

## Alternative Endpoint

Each agent also supports `/agent.json` which redirects to `/.well-known/agent.json`:

```bash
# These redirect to /.well-known/agent.json
curl http://localhost:5050/agent.json
curl http://localhost:5052/agent.json
curl http://localhost:5053/agent.json
curl http://localhost:5054/agent.json
```

## A2A Protocol Compliance

### Standard Path
The A2A protocol specifies that agents should expose their capabilities at:
```
/.well-known/agent.json
```

This follows the **RFC 8615** well-known URI standard used for service discovery.

### Features Implemented

? **Agent Metadata**
- Name, description, version
- Base URL for the agent

? **Capabilities Declaration**
- Streaming support indication
- Input/output modes

? **Skills Documentation**
- Skill ID and name
- Detailed descriptions
- Tags for categorization
- Usage examples

? **Swagger Integration**
- All endpoints documented in Swagger UI
- Interactive testing available

## Agent Discovery

Other agents can discover capabilities by:

1. **Direct URL Query**: Query the well-known endpoint
```bash
curl http://localhost:5052/.well-known/agent.json
```

2. **Discovery Service**: Use the existing discovery service
```bash
curl http://localhost:5000/list
```

3. **Dynamic Discovery**: Parse Agent Card to understand capabilities
```csharp
var httpClient = new HttpClient();
var agentCard = await httpClient.GetFromJsonAsync<AgentCard>(
    "http://localhost:5052/.well-known/agent.json");

Console.WriteLine($"Agent: {agentCard.Name}");
foreach (var skill in agentCard.Skills)
{
    Console.WriteLine($"  - {skill.Name}: {skill.Description}");
}
```

## Integration with Existing System

### Discovery Service Integration

The Agent Cards complement the existing discovery service:

- **Discovery Service** (`localhost:5000`): Central registry for agent lookup
- **Agent Cards** (`/.well-known/agent.json`): Self-describing agent metadata

Agents register with the discovery service AND expose their own Agent Card:

```
Agent starts
    ?
Register with Discovery Service ? POST /register
    ?
Expose Agent Card ? GET /.well-known/agent.json
    ?
Available for discovery via both methods
```

### Usage in Agent1 (Orchestrator)

Agent1 can now:
1. Query discovery service for available agents
2. Retrieve each agent's Card for detailed capabilities
3. Make intelligent routing decisions based on skills

```csharp
// Get agent from discovery
var agent = await discoveryClient.GetAgent("reverse");

// Get detailed capabilities from Agent Card
var agentCard = await httpClient.GetFromJsonAsync<AgentCard>(
    $"http://localhost:{agent.Port}/.well-known/agent.json");

// Use skill information for routing
var skill = agentCard.Skills.FirstOrDefault(s => s.Id == "reverse");
```

## Files Changed

? **Agent1/Program.cs** - Added Agent Card endpoint
? **Agent2/Program.cs** - Added Agent Card endpoint  
? **Agent3/Program.cs** - Added Agent Card endpoint  
? **Agent4/Program.cs** - Added Agent Card endpoint  

## Build Status

? **Build Successful**

## Benefits

### 1. **Standardization**
- Follows A2A protocol standard
- Compatible with A2A ecosystem
- Industry-standard discovery mechanism

### 2. **Self-Documentation**
- Agents describe their own capabilities
- No need for external documentation
- Machine-readable format

### 3. **Dynamic Discovery**
- Agents can discover each other
- Capabilities can change dynamically
- Version information included

### 4. **Developer Experience**
- Easy to test with curl/browser
- Swagger UI integration
- Clear examples provided

### 5. **Interoperability**
- Works with other A2A-compliant systems
- Standard JSON format
- Well-known URI pattern

## Next Steps

### Optional Enhancements

1. **Add Authentication Info**
```json
{
  "authentication": {
    "type": "bearer",
    "required": false
  }
}
```

2. **Add Rate Limiting Info**
```json
{
  "rateLimits": {
    "requests": 100,
    "period": "minute"
  }
}
```

3. **Add Health Check Endpoint**
```csharp
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
```

4. **Add Version Negotiation**
```json
{
  "supportedVersions": ["1.0.0", "1.1.0"]
}
```

## Summary

**What Was Added:** A2A standard Agent Card endpoints for all agents  
**Standard:** `/.well-known/agent.json` (RFC 8615)  
**Status:** ? Implemented and tested  
**Benefit:** Standardized, self-describing agent discovery  

**All agents now expose their capabilities following the A2A protocol standard!** ??
