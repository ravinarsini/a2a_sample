# Quick Start: Testing A2A Agent Cards

## Start All Agents

```bash
# Terminal 1 - Discovery Service
dotnet run --project Agent2AgentProtocol.Discovery.Service

# Terminal 2 - Agent2 (Reverse)
dotnet run --project Semantic.Kernel.Agent2AgentProtocol.Example.Agent2

# Terminal 3 - Agent3 (Uppercase)
dotnet run --project Semantic.Kernel.Agent2AgentProtocol.Example.Agent3

# Terminal 4 - Agent4 (News)
dotnet run --project Semantic.Kernel.Agent2AgentProtocol.Example.Agent4

# Terminal 5 - Agent1 (Orchestrator)
dotnet run --project Semantic.Kernel.Agent2AgentProtocol.Example.Agent1
```

## Test Agent Cards

### Using Browser
Open these URLs in your browser:
- http://localhost:5050/.well-known/agent.json (Agent1)
- http://localhost:5052/.well-known/agent.json (Agent2)
- http://localhost:5053/.well-known/agent.json (Agent3)
- http://localhost:5054/.well-known/agent.json (Agent4)

### Using curl
```bash
# Test all agents
curl http://localhost:5050/.well-known/agent.json | jq
curl http://localhost:5052/.well-known/agent.json | jq
curl http://localhost:5053/.well-known/agent.json | jq
curl http://localhost:5054/.well-known/agent.json | jq
```

### Using PowerShell
```powershell
# Get Agent2 Card
$card = Invoke-RestMethod "http://localhost:5052/.well-known/agent.json"
$card | ConvertTo-Json -Depth 5

# Show all skills
$card.skills | Format-Table id, name, description
```

## Access Swagger UI

Each agent has Swagger UI:
- http://localhost:5050/swagger (Agent1)
- http://localhost:5052/swagger (Agent2)
- http://localhost:5053/swagger (Agent3)
- http://localhost:5054/swagger (Agent4)

## Agent Ports Summary

| Agent | Port | Type | Agent Card |
|-------|------|------|------------|
| Discovery | 5000 | Service | N/A |
| Agent1 | 5050 | Orchestrator | /.well-known/agent.json |
| Agent2 | 5052 | Reverse | /.well-known/agent.json |
| Agent3 | 5053 | Uppercase | /.well-known/agent.json |
| Agent4 | 5054 | News | /.well-known/agent.json |

## Expected Output

Each agent logs its Agent Card URL on startup:

```
═══════════════════════════════════════════════════════════
   Agent 2 - Text Reversal Agent
═══════════════════════════════════════════════════════════
Agent Card URL: http://localhost:5052/.well-known/agent.json
Swagger UI: http://localhost:5052/swagger
Transport: a2a-reverse (Named Pipe)
Capability: reverse
═══════════════════════════════════════════════════════════
```

## Verify Implementation

Run this PowerShell script to test all agents:

```powershell
$agents = @(
    @{Name="Agent1 (Orchestrator)"; Port=5050},
    @{Name="Agent2 (Reverse)"; Port=5052},
    @{Name="Agent3 (Uppercase)"; Port=5053},
    @{Name="Agent4 (News)"; Port=5054}
)

foreach ($agent in $agents) {
    Write-Host "`n$($agent.Name)" -ForegroundColor Cyan
    Write-Host "═══════════════════════════" -ForegroundColor Cyan
    
    try {
        $card = Invoke-RestMethod "http://localhost:$($agent.Port)/.well-known/agent.json"
        Write-Host "✓ Agent Card accessible" -ForegroundColor Green
        Write-Host "  Name: $($card.name)"
        Write-Host "  Skills: $($card.skills.Count)"
        foreach ($skill in $card.skills) {
            Write-Host "    - $($skill.id): $($skill.description)"
        }
    }
    catch {
        Write-Host "✗ Agent Card not accessible" -ForegroundColor Red
        Write-Host "  Error: $($_.Exception.Message)"
    }
}
```

Save as `test-agent-cards.ps1` and run:
```powershell
.\test-agent-cards.ps1
```

## Expected Results

All agents expose Agent Cards at `/.well-known/agent.json`  
All Agent Cards return valid JSON  
All skills are documented with examples  
Swagger UI accessible for all agents  

**Your A2A Agent Cards are now live!** 🎉
