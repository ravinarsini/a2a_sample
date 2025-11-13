# Semantic Kernel Planner Integration - Architecture Update

## ?? Overview

The system has been refactored to use **Semantic Kernel's intelligent routing** instead of hardcoded string splitting logic. This makes the agents more flexible, intelligent, and easier to extend.

---

## ?? What Changed

### **Before: Hardcoded String Splitting**

```csharp
// ? OLD APPROACH - Hardcoded logic
if(text.StartsWith("reverse:", StringComparison.OrdinalIgnoreCase))
{
    string input = text["reverse:".Length..].Trim();
    // Process reverse
}
else if(text.StartsWith("upper:", StringComparison.OrdinalIgnoreCase))
{
    string input = text["upper:".Length..].Trim();
    // Process uppercase
}
```

### **After: Plugin-Based with Intelligent Routing**

```csharp
// ? NEW APPROACH - AI-powered routing
kernel.ImportPluginFromType<TextProcessingPlugin>("TextProcessing");
var router = new AgentRouter(kernel);

FunctionResult result = await router.RouteAndExecuteAsync(userRequest);
```

---

## ??? New Architecture

### **1. TextProcessingPlugin (Core/SemanticKernel/TextProcessingPlugin.cs)**

Functions are now defined as a **Plugin** with Kernel Function attributes:

```csharp
public class TextProcessingPlugin
{
    [KernelFunction("reverse_text")]
    [Description("Reverses the input text")]
    public string Reverse([Description("The text to reverse")] string input)
  {
        return new string(input.Reverse().ToArray());
}

    [KernelFunction("uppercase_text")]
    [Description("Converts the input text to uppercase")]
    public string Uppercase([Description("The text to convert")] string input)
    {
  return input.ToUpperInvariant();
    }

    [KernelFunction("search_news")]
    [Description("Searches for news using AI")]
  public async Task<string> SearchNews(
      [Description("The news topic")] string topic,
        Microsoft.SemanticKernel.Kernel kernel)
 {
      // Uses ChatGPT to search for news
    }
}
```

**Benefits:**
- ? Discoverable by Semantic Kernel
- ? Self-documenting with descriptions
- ? Type-safe parameters
- ? Easy to add new functions

---

### **2. AgentRouter (Core/SemanticKernel/AgentRouter.cs)**

Intelligent routing engine that uses AI to understand intent:

```csharp
public class AgentRouter
{
    public async Task<FunctionResult> RouteAndExecuteAsync(string userRequest)
    {
        // Uses AI to determine which function to call
        // Extracts parameters intelligently
// Executes the appropriate function
 }

    public async Task<(string functionName, string parameter)> DetermineIntentAsync(string request)
    {
 // Uses AI to parse natural language requests
    }
}
```

**Features:**
- ?? **AI-Powered Intent Detection**: Uses ChatGPT to understand what the user wants
- ?? **Fallback Logic**: Falls back to heuristics if AI routing fails
- ?? **Natural Language Support**: Handles various request formats

---

### **3. Updated Agents**

All agents now use the plugin-based approach:

#### **Agent2 (Reverse)**
```csharp
// Import plugin
kernel.ImportPluginFromType<TextProcessingPlugin>("TextProcessing");

// Initialize router
_router = new AgentRouter(kernel);

// Process requests intelligently
FunctionResult result = await _router.RouteAndExecuteAsync(text);
```

#### **Agent3 (Uppercase)**
Same pattern as Agent2

#### **Agent4 (News)**
```csharp
// Determine intent
(string functionName, string parameter) = await _router.DetermineIntentAsync(text);

// Execute function
KernelPlugin plugin = kernel.Plugins["TextProcessing"];
KernelFunction function = plugin[functionName];
FunctionResult result = await kernel.InvokeAsync(function, arguments);

// Special handling for news (file creation)
if (functionName == "search_news")
{
    // Create file with results
}
```

---

## ?? New Capabilities

### **Natural Language Requests**

Users can now send natural language requests instead of structured commands:

#### **Old Format (Still Supported)**
```bash
curl -X POST http://localhost:5050/api/client/post \
  -d "\"reverse: hello world\""
```

#### **New Natural Language Format**
```bash
curl -X POST http://localhost:5050/api/client/post \
  -d "\"Can you reverse this text: hello world\""

curl -X POST http://localhost:5050/api/client/post \
  -d "\"Please find news about artificial intelligence\""

curl -X POST http://localhost:5050/api/client/post \
  -d "\"Make this uppercase: test\""
```

---

## ?? Request Flow

```
???????????????????????????????????????????????????
?  User sends natural language request      ?
???????????????????????????????????????????????????
  ?
???????????????????????????????????????????????????
?  Agent1 (Client API)        ?
?  - Simple keyword extraction   ?
?  - Routes to appropriate agent      ?
???????????????????????????????????????????????????
        ?
???????????????????????????????????????????????????
?  Agent2/3/4 receives request    ?
?  - Imports TextProcessingPlugin         ?
?  - Creates AgentRouter      ?
???????????????????????????????????????????????????
              ?
???????????????????????????????????????????????????
?  AgentRouter analyzes request ?
?  - Uses AI to understand intent         ?
?  - Extracts parameters       ?
?  - Determines function to call    ?
???????????????????????????????????????????????????
     ?
???????????????????????????????????????????????????
?  Semantic Kernel executes function ?
?  - Calls appropriate plugin method    ?
?  - Passes extracted parameters        ?
???????????????????????????????????????????????????
       ?
???????????????????????????????????????????????????
?  Result returned to Agent1   ?
?  - Agent processes result         ?
?  - Returns to user      ?
???????????????????????????????????????????????????
```

---

## ?? Testing Examples

### **Example 1: Reverse Text**
```bash
# Old format
curl -X POST http://localhost:5050/api/client/post \
  -H "Content-Type: application/json" \
  -d "\"reverse: hello world\""

# New natural language format
curl -X POST http://localhost:5050/api/client/post \
  -H "Content-Type: application/json" \
  -d "\"Please reverse this: hello world\""
```

### **Example 2: Uppercase**
```bash
# Old format
curl -X POST http://localhost:5050/api/client/post \
  -H "Content-Type: application/json" \
  -d "\"uppercase: test\""

# New natural language format
curl -X POST http://localhost:5050/api/client/post \
  -H "Content-Type: application/json" \
  -d "\"Convert to caps: test\""
```

### **Example 3: News Search**
```bash
# Old format
curl -X POST http://localhost:5050/api/client/post \
  -H "Content-Type: application/json" \
  -d "\"news: artificial intelligence\""

# New natural language format
curl -X POST http://localhost:5050/api/client/post \
  -H "Content-Type: application/json" \
  -d "\"Find me the latest news about AI\""
```

---

## ?? How to Add New Capabilities

### **Step 1: Add Function to Plugin**

```csharp
// In TextProcessingPlugin.cs
[KernelFunction("translate_text")]
[Description("Translates text to another language")]
public async Task<string> Translate(
    [Description("The text to translate")] string text,
 [Description("Target language")] string language,
    Microsoft.SemanticKernel.Kernel kernel)
{
    string prompt = $"Translate '{text}' to {language}";
    var result = await kernel.InvokePromptAsync(prompt);
    return result.ToString();
}
```

### **Step 2: Update Capability Cards**

```json
// In news.card.json or create translate.card.json
{
    "agentId": "Agent5",
  "name": "translate",
    "skill": "translate",
    "capabilities": [
  {
   "name": "translate",
   "description": "Translates text to another language"
        }
    ]
}
```

### **Step 3: That's It!**

The AgentRouter will automatically:
- Discover the new function
- Route requests intelligently
- Handle natural language requests

---

## ?? Debugging

### **Enable Detailed Logging**

```csharp
// In Agent2/3/4
_logger.LogInformation("[Agent-X] Router determined: {Function} with {Parameter}", 
    functionName, parameter);
```

### **Test Router Directly**

```csharp
var router = new AgentRouter(kernel);
var (function, param) = await router.DetermineIntentAsync("reverse hello");
Console.WriteLine($"Function: {function}, Param: {param}");
```

---

## ?? Configuration

### **OpenAI API Key (Required for AI Routing)**

Agent4 already has OpenAI configured. To enable AI routing in Agent2/3:

```csharp
// In Agent2/3 Program.cs
services.AddSingleton<Kernel>(sp =>
{
    var builder = Kernel.CreateBuilder();
    builder.AddOpenAIChatCompletion(
        modelId: "gpt-4",
        apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
    return builder.Build();
});
```

---

## ?? Benefits

| Feature | Before | After |
|---------|--------|-------|
| **Extensibility** | Add if/else for each new command | Just add `[KernelFunction]` |
| **Flexibility** | Exact format required | Natural language supported |
| **Maintainability** | Scattered logic | Centralized in plugin |
| **Testability** | Hard to unit test | Easy to test individual functions |
| **Discoverability** | Hidden in code | Self-documented with attributes |
| **AI Integration** | Manual | Automatic via Semantic Kernel |

---

## ?? Migration Notes

### **Backward Compatibility**

The old `TextProcessingFunction.GetFunctionByType()` is still available but **marked as obsolete**:

```csharp
[Obsolete("Use TextProcessingPlugin with kernel.ImportPluginFromType instead")]
public static class TextProcessingFunction
{
  // Legacy support
}
```

### **Gradual Migration**

You can migrate agents one by one:
1. Agent2 ? Updated
2. Agent3 ? Updated
3. Agent4 ? Updated
4. Legacy code still works via obsolete methods

---

## ?? Future Enhancements

### **1. Advanced Planners**

```csharp
// Use Semantic Kernel's built-in planners
var planner = new HandlebarsPlanner();
var plan = await planner.CreatePlanAsync(kernel, "Search for news and reverse the headlines");
await plan.InvokeAsync(kernel);
```

### **2. Multi-Step Workflows**

```csharp
[KernelFunction("news_and_summarize")]
public async Task<string> NewsAndSummarize(string topic, Kernel kernel)
{
    // 1. Search for news
 var news = await SearchNews(topic, kernel);
    
    // 2. Summarize the results
    var summary = await kernel.InvokePromptAsync($"Summarize: {news}");
    
   return summary.ToString();
}
```

### **3. Agent Collaboration**

```csharp
// Agent4 could call Agent2 to reverse headlines
public async Task<string> ReverseNews(string topic)
{
    var news = await SearchNews(topic, kernel);
 // Call Agent2 to reverse
    var reversed = await CallAgent2(news);
    return reversed;
}
```

---

## ? Verification

Build status: **SUCCESS** ?

All agents now use:
- ? Plugin-based architecture
- ? Intelligent routing via AgentRouter
- ? Natural language support
- ? Extensible design

---

**Version:** 2.0.0  
**Updated:** January 2025  
**Status:** ? Production Ready
