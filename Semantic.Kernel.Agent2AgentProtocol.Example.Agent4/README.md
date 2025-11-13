# Agent 4 - News Search Agent

## Overview

Agent4 is a news search agent that uses ChatGPT to search for the latest news about a given topic and automatically creates a text file with the results.

## Features

- ?? **AI-Powered News Search**: Uses ChatGPT (GPT-4) to find and summarize recent news
- ?? **Automatic File Creation**: Saves results to timestamped files
- ?? **A2A Protocol Integration**: Communicates with other agents via the discovery service
- ?? **Async Message Processing**: Handles requests asynchronously

## Setup

### Prerequisites

- .NET 8.0 SDK
- OpenAI API Key

### Configuration

1. **Set your OpenAI API Key** (choose one method):

   **Option A: Environment Variable**
   ```bash
   # Windows (PowerShell)
   $env:OPENAI_API_KEY="your-api-key-here"
   
   # Linux/macOS
   export OPENAI_API_KEY="your-api-key-here"
   ```

   **Option B: appsettings.json**
   ```json
   {
     "OpenAI": {
       "ApiKey": "your-api-key-here",
       "ModelId": "gpt-4"
     }
   }
   ```

2. **Build the project**
   ```bash
   dotnet build
   ```

## Running Agent4

### Standalone
```bash
dotnet run --project Semantic.Kernel.Agent2AgentProtocol.Example.Agent4
```

### With Full System

1. Start the Discovery Service:
```bash
   dotnet run --project Agent2AgentProtocol.Discovery.Service
   ```

2. Start Agent4:
   ```bash
   dotnet run --project Semantic.Kernel.Agent2AgentProtocol.Example.Agent4
   ```

3. Start Agent1 (Client):
   ```bash
   dotnet run --project Semantic.Kernel.Agent2AgentProtocol.Example.Agent1
   ```

4. Send a news search request via the Agent1 API:
   ```bash
   curl -X POST http://localhost:5050/api/client/post \
 -H "Content-Type: application/json" \
 -d "\"news: artificial intelligence\""
```

## Usage

### Request Format

Send requests in the format: `news: <topic>`

**Examples:**
- `news: artificial intelligence`
- `news: climate change`
- `news: stock market`
- `news: sports`

### Output

Agent4 will:
1. Search for news using ChatGPT
2. Create a file in the `NewsResults` directory
3. File naming pattern: `news_<topic>_<timestamp>.txt`
4. Return the file path and content in the response

**Example Output File:**
```
NewsResults/news_artificial_intelligence_20250101_143022.txt
```

## Architecture

```
Agent1 (Client)
    ? "news: AI"
Discovery Service ? Resolves "news" ? Agent4
    ?
Agent4 receives request
    ?
Semantic Kernel ? ChatGPT API
    ?
News results formatted
    ?
File created in NewsResults/
    ?
Response sent back to Agent1
```

## Capability Card

Agent4 registers with the discovery service using `news.card.json`:

```json
{
    "agentId": "Agent4",
    "name": "news",
    "skill": "news",
    "Address": "a2a-news",
  "capabilities": [
        {
            "name": "news",
            "description": "Searches for news using ChatGPT and creates a file with results"
   }
    ]
}
```

## Transport

- **Type**: Named Pipe
- **Pipe Name**: `a2a-news`
- **Mode**: Server (listens for connections)

## Error Handling

- Invalid API key: Agent will start but warn that news functionality won't work
- Network errors: Returned in response message
- File I/O errors: Logged and returned in response

## Extending Agent4

### Add More Capabilities

Edit `TextProcessingFunction.cs` to add new functions:

```csharp
"WEATHER" => KernelFunctionFactory.CreateFromMethod(SearchWeather, ...),
```

### Change Output Format

Modify the prompt in `SearchNews()` method to customize the news format.

### Add Different AI Models

Update `appsettings.json`:
```json
{
  "OpenAI": {
    "ModelId": "gpt-3.5-turbo"  // or "gpt-4o"
  }
}
```

## Troubleshooting

### "OpenAI API Key not found"
- Verify your API key is set in environment variables or appsettings.json
- Restart the agent after setting the key

### "Request timed out"
- News searches may take longer (up to 60 seconds)
- Check your internet connection
- Verify OpenAI service status

### "File creation failed"
- Ensure write permissions in the agent directory
- Check disk space availability

## Files Created by Agent4

All news result files are saved in:
```
NewsResults/
??? news_artificial_intelligence_20250101_143022.txt
??? news_climate_change_20250101_143045.txt
??? news_stock_market_20250101_143108.txt
```

## Integration with Other Agents

Agent4 can be combined with other agents:

```bash
# Get news and then reverse it (silly example)
curl -X POST http://localhost:5050/api/client/post \
  -H "Content-Type: application/json" \
  -d "\"news: technology\""
```

The response can then be piped to Agent2 or Agent3 for further processing.
