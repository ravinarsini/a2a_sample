# ?? Agent4 Quick Reference Card

## Start All Services (5 Terminals)

```bash
# Terminal 1 - Discovery Service
dotnet run --project Agent2AgentProtocol.Discovery.Service

# Terminal 2 - Agent2 (Reverse)
dotnet run --project Semantic.Kernel.Agent2AgentProtocol.Example.Agent2

# Terminal 3 - Agent3 (Uppercase)
dotnet run --project Semantic.Kernel.Agent2AgentProtocol.Example.Agent3

# Terminal 4 - Agent4 (News) ? NEW
dotnet run --project Semantic.Kernel.Agent2AgentProtocol.Example.Agent4

# Terminal 5 - Agent1 (Client API)
dotnet run --project Semantic.Kernel.Agent2AgentProtocol.Example.Agent1
```

## ?? Configure OpenAI API Key (Required!)

```powershell
# PowerShell
$env:OPENAI_API_KEY="sk-your-key-here"

# CMD
set OPENAI_API_KEY=sk-your-key-here

# Linux/macOS
export OPENAI_API_KEY="sk-your-key-here"
```

## ?? Test Agent4

### Using curl
```bash
curl -X POST http://localhost:5050/api/client/post \
  -H "Content-Type: application/json" \
  -d "\"news: artificial intelligence\""
```

### Using PowerShell
```powershell
Invoke-RestMethod -Uri "http://localhost:5050/api/client/post" `
  -Method Post `
  -ContentType "application/json" `
  -Body '"news: climate change"'
```

### Using Swagger UI
```
http://localhost:5050/swagger
```

### Using Test Script
```powershell
.\Test-Agent4.ps1
```

## ?? Available Commands

| Command | Agent | Description |
|---------|-------|-------------|
| `reverse: text` | Agent2 | Reverses input text |
| `uppercase: text` | Agent3 | Converts to uppercase |
| **`news: topic`** | **Agent4** | **Searches news & creates file** ? |

## ?? Check Agent4 Status

### Discovery Service
```bash
curl http://localhost:5000/list
```

### Should see Agent4 registered:
```json
{
  "news": {
    "skill": "news",
    "address": "a2a-news"
  }
}
```

## ?? Output Files

Location: `Semantic.Kernel.Agent2AgentProtocol.Example.Agent4\NewsResults\`

Format: `news_<topic>_<yyyyMMdd_HHmmss>.txt`

Example: `news_artificial_intelligence_20250101_143022.txt`

## ?? Troubleshooting

| Issue | Solution |
|-------|----------|
| "OpenAI API Key not found" | Set `OPENAI_API_KEY` environment variable |
| "Request timed out" | Wait up to 60 seconds for news searches |
| "Agent4 not registered" | Check Agent4 console for registration errors |
| "File not created" | Check write permissions in Agent4 directory |
| Discovery Service error | Verify it's running on port 5000 |

## ?? System Ports

| Service | Port/Address |
|---------|--------------|
| Discovery Service | `http://localhost:5000` |
| Agent1 API | `http://localhost:5050` |
| Agent2 | Named Pipe: `a2a-reverse` |
| Agent3 | Named Pipe: `a2a-uppercase` |
| **Agent4** | **Named Pipe: `a2a-news`** ? |

## ?? Example Queries

```bash
# Technology news
"news: artificial intelligence"
"news: blockchain"
"news: quantum computing"

# Business news
"news: stock market"
"news: cryptocurrency"
"news: economy"

# Science news
"news: climate change"
"news: space exploration"
"news: medical research"

# Entertainment
"news: movies"
"news: sports"
"news: gaming"
```

## ?? Expected Response

```json
{
  "keyword": "news",
  "matchedCount": 1,
  "responses": [
    {
      "agent": "news",
      "endpoint": "a2a-news",
   "response": "News search completed. File created: ...\n\nContent:\n- Date: Headline...",
      "success": true
    }
  ]
}
```

## ?? Documentation Files

- `AGENT4_SETUP.md` - Complete setup guide
- `AGENT4_IMPLEMENTATION_SUMMARY.md` - Technical details
- `Semantic.Kernel.Agent2AgentProtocol.Example.Agent4\README.md` - Agent4 docs
- `Test-Agent4.ps1` - Automated test script

## ? Quick Commands

```powershell
# Build
dotnet build

# Run all tests
.\Test-Agent4.ps1

# Check logs
# See Agent4 console output

# View results
cd Semantic.Kernel.Agent2AgentProtocol.Example.Agent4\NewsResults
dir

# Clean results
Remove-Item NewsResults\*.txt
```

## ?? Security Notes

- ? Never commit API keys to Git
- ? Use environment variables
- ? Add `appsettings.json` to `.gitignore` if it contains secrets
- ? Use Azure Key Vault in production

## ?? Learning Resources

- [Semantic Kernel Docs](https://learn.microsoft.com/semantic-kernel/)
- [A2A Protocol](https://github.com/a2aproject/a2a-dotnet)
- [OpenAI API](https://platform.openai.com/docs)

---

**Status:** ? Ready  
**Version:** 1.0.0  
**Build:** ? Success  

**Created by:** GitHub Copilot  
**Date:** January 2025

---

?? **Tip:** Keep this file open for quick reference while testing!
