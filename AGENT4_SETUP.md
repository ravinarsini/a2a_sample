# Agent4 Setup Guide

## Quick Start

### 1. Add Agent4 to Solution

If using Visual Studio, add the Agent4 project to your solution file. Otherwise, you can run it standalone.

### 2. Configure OpenAI API Key

**Required for Agent4 to function!**

Set your OpenAI API key using one of these methods:

#### Method 1: Environment Variable (Recommended)
```powershell
# Windows PowerShell
$env:OPENAI_API_KEY="sk-your-api-key-here"

# Windows CMD
set OPENAI_API_KEY=sk-your-api-key-here

# Linux/macOS
export OPENAI_API_KEY="sk-your-api-key-here"
```

#### Method 2: appsettings.json
Edit `Semantic.Kernel.Agent2AgentProtocol.Example.Agent4\appsettings.json`:
```json
{
  "OpenAI": {
    "ApiKey": "sk-your-api-key-here",
    "ModelId": "gpt-4"
  }
}
```

### 3. Build All Projects
```bash
dotnet build
```

### 4. Run All Agents

**Terminal 1 - Discovery Service:**
```bash
dotnet run --project Agent2AgentProtocol.Discovery.Service
```
Listen on: `http://localhost:5000`

**Terminal 2 - Agent2 (Reverse):**
```bash
dotnet run --project Semantic.Kernel.Agent2AgentProtocol.Example.Agent2
```
Capability: `reverse`

**Terminal 3 - Agent3 (Uppercase):**
```bash
dotnet run --project Semantic.Kernel.Agent2AgentProtocol.Example.Agent3
```
Capability: `uppercase`

**Terminal 4 - Agent4 (News Search):**
```bash
dotnet run --project Semantic.Kernel.Agent2AgentProtocol.Example.Agent4
```
Capability: `news`

**Terminal 5 - Agent1 (Client API):**
```bash
dotnet run --project Semantic.Kernel.Agent2AgentProtocol.Example.Agent1
```
API: `http://localhost:5050`

### 5. Test Agent4

Open browser or use curl:

**Using Swagger UI:**
```
http://localhost:5050/swagger
```

**Using curl:**
```bash
# Search for AI news
curl -X POST http://localhost:5050/api/client/post \
  -H "Content-Type: application/json" \
  -d "\"news: artificial intelligence\""

# Search for climate change news
curl -X POST http://localhost:5050/api/client/post \
  -H "Content-Type: application/json" \
  -d "\"news: climate change\""
```

**Using PowerShell:**
```powershell
Invoke-RestMethod -Uri "http://localhost:5050/api/client/post" `
  -Method Post `
  -ContentType "application/json" `
  -Body '"news: technology"'
```

## Visual Studio Multi-Project Startup

To run all projects simultaneously in Visual Studio:

1. Right-click on the Solution in Solution Explorer
2. Select "Configure Startup Projects..."
3. Choose "Multiple startup projects"
4. Set Action to "Start" for:
   - Agent2AgentProtocol.Discovery.Service
   - Semantic.Kernel.Agent2AgentProtocol.Client (Agent1)
   - Semantic.Kernel.Agent2AgentProtocol.ReverseAgent (Agent2)
   - Semantic.Kernel.Agent2AgentProtocol.UpperCaseAgent (Agent3)
   - Semantic.Kernel.Agent2AgentProtocol.NewsAgent (Agent4)
5. Click OK
6. Press F5 to start all projects

## Expected Output

### Agent4 Console Output:
```
???????????????????????????????????????????????????????????
 Agent 4 - News Search Agent
???????????????????????????????????????????????????????????
Listening on: a2a-news
Capability: news
Usage: Send 'news: <topic>' to search for news
???????????????????????????????????????????????????????????

info: Semantic.Kernel.Agent2AgentProtocol.Example.Agent4.Agent4[0]
      [Agent-4] News Agent waiting for tasks...
info: Semantic.Kernel.Agent2AgentProtocol.Example.Agent4.Agent4[0]
      [Agent-4] Registered news capability with discovery service
```

### API Response Example:
```json
{
  "keyword": "news",
  "matchedCount": 1,
  "responses": [
    {
      "agent": "news",
  "endpoint": "a2a-news",
 "response": "News search completed. File created: D:\\...\\NewsResults\\news_artificial_intelligence_20250101_143022.txt\n\nContent:\n- January 1, 2025: OpenAI Announces GPT-5...",
    "success": true
}
  ]
}
```

### Created Files:
Check the `NewsResults` folder in Agent4's directory:
```
Semantic.Kernel.Agent2AgentProtocol.Example.Agent4/
??? NewsResults/
    ??? news_artificial_intelligence_20250101_143022.txt
    ??? news_climate_change_20250101_144530.txt
    ??? news_stock_market_20250101_145612.txt
```

## System Architecture

```
???????????????????????????????????????????????????????????
?    Discovery Service (Port 5000)            ?
?  - Registers agent capabilities        ?
?  - Resolves agents by skill?
???????????????????????????????????????????????????????????
   ?
             ?
   ???????????????????????????????????????????????????
        ?      ?        ?           ?
  ?????????????    ?????????????    ?????????????  ?????????????
  ?  Agent 2  ?    ?  Agent 3  ?    ?  Agent 4  ?  ?  Agent 1  ?
  ? (Reverse) ?    ?(Uppercase)?    ?  (News)   ?  ? (Client)  ?
  ?   ?    ?    ?    ?     ?  ?API?
  ? Named?  ? Named     ?    ? Named     ?  ? Port 5050 ?
  ? Pipe      ? ? Pipe ?    ? Pipe      ?  ?      ?
  ?????????????    ?????????????    ?????????????  ?????????????
  ?
       ????????????
         ? ChatGPT  ?
 ?  GPT-4   ?
          ????????????
```

## Troubleshooting

### Agent4 Won't Start
- Check if OpenAI API key is set
- Verify .NET 8.0 SDK is installed
- Ensure port `a2a-news` named pipe is available

### No Response from Agent4
- Check if Discovery Service is running
- Verify Agent4 registered successfully (check Agent4 console)
- Increase timeout in Agent1 (already set to 60 seconds for news)
- Check OpenAI API quota/billing

### File Not Created
- Verify write permissions in Agent4 directory
- Check disk space
- Look for errors in Agent4 console logs

## Advanced Configuration

### Change ChatGPT Model
Edit `appsettings.json`:
```json
{
  "OpenAI": {
    "ModelId": "gpt-3.5-turbo"  // Faster, cheaper
    // or "gpt-4o"  // More capable
  }
}
```

### Change Output Directory
Modify `Agent4.cs`:
```csharp
string filePath = Path.Combine(
    Directory.GetCurrentDirectory(), 
 "CustomOutputFolder",  // Change this
    fileName
);
```

### Customize News Format
Edit the prompt in `TextProcessingFunction.cs` ? `SearchNews()` method.

## Available Agents

| Agent | Capability | Description |
|-------|-----------|-------------|
| Agent1 | client | Client API for sending requests |
| Agent2 | reverse | Reverses input text |
| Agent3 | uppercase | Converts text to uppercase |
| **Agent4** | **news** | **Searches news and creates file** |

## Next Steps

1. ? Configure OpenAI API key
2. ? Build all projects
3. ? Start all agents
4. ? Test with news search
5. ?? Extend with more capabilities!

For detailed Agent4 documentation, see: `Semantic.Kernel.Agent2AgentProtocol.Example.Agent4\README.md`
