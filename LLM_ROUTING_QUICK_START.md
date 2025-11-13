# Quick Reference: LLM-Based Routing

## Setup (2 Steps)

### 1. Set OpenAI API Key

**Windows:**
```powershell
$env:OPENAI_API_KEY="sk-your-key-here"
```

**Linux/macOS:**
```bash
export OPENAI_API_KEY="sk-your-key-here"
```

**Or edit `Agent1/appsettings.json`:**
```json
{
  "OpenAI": {
    "ApiKey": "sk-your-key-here"
  }
}
```

### 2. Start Agent1
```bash
dotnet run --project Semantic.Kernel.Agent2AgentProtocol.Example.Agent1
```

---

## How It Works

```
User Input ? LLM Analysis ? Extract (Skill + Content) ? Route to Agent
```

**Example:**
```
"reverse below string Hi Ravi?"
? LLM determines: skill=reverse, content=Hi Ravi?
? Sends to Agent2: "Hi Ravi?"
? Response: "?ivaR iH"
```

---

## Test It

```bash
# Test reverse
curl -X POST http://localhost:5050/api/client/post \
  -H "Content-Type: application/json" \
  -d '"reverse below string Hi Ravi?"'

# Test news
curl -X POST http://localhost:5050/api/client/post \
  -H "Content-Type: application/json" \
  -d '"find news about AI"'

# Test uppercase
curl -X POST http://localhost:5050/api/client/post \
  -H "Content-Type: application/json" \
  -d '"make this UPPERCASE: hello"'
```

---

## Features

? Natural language understanding  
? Automatic content extraction  
? Context-aware routing  
? Fallback to keyword matching  
? Multiline support  

---

## Response Format

```json
{
  "request": "reverse below string Hi Ravi?",
  "determinedSkill": "reverse",
  "extractedContent": "Hi Ravi?",
  "responses": [
    {
      "agent": "reverse",
      "response": "?ivaR iH",
      "success": true
    }
  ]
}
```

---

## Troubleshooting

**No API Key?**  
Falls back to keyword matching automatically.

**LLM Too Slow?**  
Normal: 1-3 seconds for routing decision.

**Wrong Agent?**  
LLM learns from available agents - check discovery service.

---

**Documentation:** See `LLM_BASED_AGENT_ROUTING.md` for full details.
