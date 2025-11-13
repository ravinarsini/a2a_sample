# Quick Start: Static Registry (No Discovery Service)

## **Discovery Service Removed - Static Registry Active**

---

## Start System (4 Terminals)

```bash
# Terminal 1 - Agent2 (Reverse)
dotnet run --project Semantic.Kernel.Agent2AgentProtocol.Example.Agent2

# Terminal 2 - Agent3 (Uppercase)
dotnet run --project Semantic.Kernel.Agent2AgentProtocol.Example.Agent3

# Terminal 3 - Agent4 (News)
dotnet run --project Semantic.Kernel.Agent2AgentProtocol.Example.Agent4

# Terminal 4 - Agent1 (Orchestrator)
dotnet run --project Semantic.Kernel.Agent2AgentProtocol.Example.Agent1
```

**NO Discovery Service needed!** ❌ `Agent2AgentProtocol.Discovery.Service`

---

## Quick Tests

### List Available Agents
```bash
curl http://localhost:5050/api/agents | jq
```

### Reverse Text
```bash
curl -X POST http://localhost:5050/api/client/post \
  -H "Content-Type: application/json" \
  -d '{"prompt": "reverse: Hello World"}'
```

### Uppercase Text
```bash
curl -X POST http://localhost:5050/api/client/post \
  -H "Content-Type: application/json" \
  -d '{"prompt": "make this UPPERCASE: hello world"}'
```

### Search News
```bash
curl -X POST http://localhost:5050/api/client/post \
  -H "Content-Type: application/json" \
  -d '{"prompt": "find news about AI"}'
```

---

## What Changed

| Aspect | Before | After |
|--------|--------|-------|
| **Terminals needed** | 5 | 4 |
| **Discovery Service** | Required | Not needed |
| **Agent registration** | HTTP POST on startup | Static registry |
| **Agent lookup** | HTTP GET | In-memory |
| **Startup time** | Slower | Faster |
| **Failure points** | More | Fewer |

---

## Key Endpoints

- **Agent1 Orchestrator:** http://localhost:5050
  - Swagger: http://localhost:5050/swagger
  - Agent Card: http://localhost:5050/.well-known/agent.json
  - List Agents: http://localhost:5050/api/agents
  - Execute: POST http://localhost:5050/api/client/post

- **Agent2 Reverse:** http://localhost:5052
  - Agent Card: http://localhost:5052/.well-known/agent.json

- **Agent3 Uppercase:** http://localhost:5053
  - Agent Card: http://localhost:5053/.well-known/agent.json

- **Agent4 News:** http://localhost:5054
  - Agent Card: http://localhost:5054/.well-known/agent.json

---

## Expected Console Output

### Agent1
```
═══════════════════════════════════════════════════════════
   Agent 1 - Orchestrator Agent
═══════════════════════════════════════════════════════════
Agent Card URL: http://localhost:5050/.well-known/agent.json
Swagger UI: http://localhost:5050/swagger
API Endpoint: http://localhost:5050/api/client/post
Using: Static Agent Registry (in-memory)
Available Agents: reverse, uppercase, news
═══════════════════════════════════════════════════════════
```

### Agent2/3/4
```
═══════════════════════════════════════════════════════════
   Agent X - [Name]
═══════════════════════════════════════════════════════════
Agent Card URL: http://localhost:505X/.well-known/agent.json
Swagger UI: http://localhost:505X/swagger
Transport: a2a-[skill] (Named Pipe)
Capability: [skill]
Registry: Static (no dynamic registration needed)
═══════════════════════════════════════════════════════════
```

---

## Troubleshooting

**Error:** "No agents available in static registry"
- **Fix:** Rebuild solution: `dotnet build`

**Error:** "No agent found for skill: X"
- **Fix:** Check `AgentRegistry.cs` contains the skill

**Error:** "Request timed out"
- **Fix:** Ensure target agent is running

---

## Architecture

```
AgentRegistry (Static)
    ↓
Agent1 (In-Memory Lookup)
    ↓
Agent2/3/4 (Named Pipe)
```

**No Discovery Service = Simpler & Faster!** ✅

---

**Full Documentation:** See `MIGRATION_COMPLETE_STATIC_REGISTRY.md`
