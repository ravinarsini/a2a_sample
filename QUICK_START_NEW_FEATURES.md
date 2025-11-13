# ?? Quick Start: Using the New Semantic Kernel Planner

## What Changed?

Your agents now use **AI-powered intelligent routing** instead of hardcoded string parsing!

---

## ? What Still Works (Backward Compatible)

```bash
# All old commands still work exactly the same
curl -X POST http://localhost:5050/api/client/post \
  -H "Content-Type: application/json" \
  -d "\"reverse: hello world\""

curl -X POST http://localhost:5050/api/client/post \
  -H "Content-Type: application/json" \
  -d "\"uppercase: test\""

curl -X POST http://localhost:5050/api/client/post \
  -H "Content-Type: application/json" \
  -d "\"news: artificial intelligence\""
```

---

## ?? What's New

### Natural Language Support!

```bash
# Now you can use natural language
curl -X POST http://localhost:5050/api/client/post \
  -H "Content-Type: application/json" \
  -d "\"Can you reverse this text: hello world?\""

curl -X POST http://localhost:5050/api/client/post \
  -H "Content-Type: application/json" \
-d "\"Please convert to uppercase: test\""

curl -X POST http://localhost:5050/api/client/post \
  -H "Content-Type: application/json" \
  -d "\"Find me the latest news about AI\""
```

---

## ?? How It Works Now

### Before (Hardcoded)
```
"reverse: hello" ? Split by ':' ? Extract "hello" ? Execute
```

### After (Intelligent)
```
"Can you reverse hello?" ? AI understands intent ? Extracts parameters ? Execute
```

---

## ?? Test It Now

### Step 1: Start All Services

```bash
# Terminal 1 - Discovery
dotnet run --project Agent2AgentProtocol.Discovery.Service

# Terminal 2 - Agent2 (Reverse)
dotnet run --project Semantic.Kernel.Agent2AgentProtocol.Example.Agent2

# Terminal 3 - Agent3 (Uppercase)
dotnet run --project Semantic.Kernel.Agent2AgentProtocol.Example.Agent3

# Terminal 4 - Agent4 (News)
dotnet run --project Semantic.Kernel.Agent2AgentProtocol.Example.Agent4

# Terminal 5 - Agent1 (Client API)
dotnet run --project Semantic.Kernel.Agent2AgentProtocol.Example.Agent1
```

### Step 2: Try Natural Language Requests

```bash
# Test reverse
curl -X POST http://localhost:5050/api/client/post \
  -H "Content-Type: application/json" \
  -d "\"Please reverse this: Hello World\""

# Test uppercase
curl -X POST http://localhost:5050/api/client/post \
  -H "Content-Type: application/json" \
  -d "\"Convert to caps: test message\""

# Test news search
curl -X POST http://localhost:5050/api/client/post \
  -H "Content-Type: application/json" \
  -d "\"Search for news about technology\""
```

---

## ?? Key Files

| File | What It Does |
|------|--------------|
| `TextProcessingPlugin.cs` | Contains all agent functions with `[KernelFunction]` |
| `AgentRouter.cs` | AI-powered routing logic |
| `Agent2/3/4.cs` | Updated to use plugins and router |

---

## ?? Adding Your Own Function

### Step 1: Add to Plugin
```csharp
// In TextProcessingPlugin.cs
[KernelFunction("my_function")]
[Description("What my function does")]
public string MyFunction([Description("Input")] string input)
{
  return input.ToUpper(); // Your logic
}
```

### Step 2: That's It!
The router will automatically discover and route to it.

### Step 3: Test It
```bash
curl -X POST http://localhost:5050/api/client/post \
  -H "Content-Type: application/json" \
  -d "\"Use my function on: test\""
```

---

## ?? Debugging

### Check Agent Logs
```
[Agent-2] received: 'Can you reverse this: hello' from Agent1
[Agent-2] Router determined: reverse_text with hello
[Agent-2] Successfully processed request using router
[Agent-2] ? responding with 'olleh'
```

### Test Router Directly
```csharp
var router = new AgentRouter(kernel);
var (function, param) = await router.DetermineIntentAsync("reverse: hello");
Console.WriteLine($"Function: {function}, Param: {param}");
// Output: Function: reverse_text, Param: hello
```

---

## ?? Configuration

### Required for Agent4 (Already Done)
```json
// appsettings.json
{
  "OpenAI": {
    "ApiKey": "your-key-here",
    "ModelId": "gpt-4"
  }
}
```

### Optional: Enable AI for Agent2/3
By default, Agent2/3 use fallback heuristics (fast, no API calls).
To enable AI routing, add OpenAI to their kernels.

---

## ?? Troubleshooting

| Issue | Solution |
|-------|----------|
| "Unable to determine action" | Use clearer language or use old format `command: text` |
| "Function not found" | Ensure plugin is imported: `kernel.ImportPluginFromType<TextProcessingPlugin>()` |
| AI routing not working | Check OpenAI API key configuration |
| Old format not working | Report bug - backward compatibility should work |

---

## ?? Documentation

- **Complete Guide**: `SEMANTIC_KERNEL_PLANNER_REFACTORING.md`
- **Architecture**: `ARCHITECTURE_DIAGRAMS.md`
- **Summary**: `REFACTORING_SUMMARY.md`

---

## ?? Examples

### Example 1: Multiple Ways to Reverse
```bash
# All of these work now:
"reverse: hello"
"Please reverse: hello"
"Can you reverse this: hello"
"Reverse the text hello"
```

### Example 2: Natural News Search
```bash
# All of these work:
"news: AI"
"Find news about AI"
"Search for AI news"
"What's the latest on AI?"
"Get me news about artificial intelligence"
```

### Example 3: Flexible Uppercase
```bash
# All of these work:
"uppercase: test"
"Convert to uppercase: test"
"Make this CAPS: test"
"Capitalize test"
```

---

## ? Verification

Run this to verify everything works:

```bash
# Test all three capabilities
curl -X POST http://localhost:5050/api/client/post \
  -H "Content-Type: application/json" \
  -d "\"reverse: test\""

curl -X POST http://localhost:5050/api/client/post \
  -H "Content-Type: application/json" \
  -d "\"uppercase: test\""

curl -X POST http://localhost:5050/api/client/post \
  -H "Content-Type: application/json" \
  -d "\"news: technology\""
```

Expected: All three should work successfully.

---

## ?? You're Done!

Your system now has:
- ? AI-powered routing
- ? Natural language support
- ? Backward compatibility
- ? Easier extensibility

**Keep coding! ??**
