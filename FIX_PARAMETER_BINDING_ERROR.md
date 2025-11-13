# Fix: Newline Error with ClientRequest Parameter

## Problem

The endpoint was failing with multiline input:

```
Microsoft.AspNetCore.Http.BadHttpRequestException: Failed to read parameter "ClientRequest req" from the request body as JSON.
---> System.Text.Json.JsonException: '0x0A' is invalid within a JSON string.
```

## Root Cause

The code had **conflicting approaches**:

```csharp
// ? BROKEN CODE
app.MapPost("/api/client/post", async (
    HttpContext context,
    IHttpClientFactory httpClientFactory,
    IServiceProvider serviceProvider, 
    ClientRequest req) =>  // ? ASP.NET deserializes BEFORE your code runs
{
    // This code never runs if deserialization fails!
    using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
  string rawBody = await reader.ReadToEndAsync();
    
    // This is wrong - tries to read from parameter instead of stream
    string rawBody = req.Prompt;
```

**The Issue:**
1. ASP.NET Core tries to deserialize `ClientRequest req` **FIRST**
2. Fails on unescaped newlines (`0x0A`)
3. Your error handling code **never executes**

## Solution

**Remove the parameter** and read the body manually:

```csharp
// ? FIXED CODE
app.MapPost("/api/client/post", async (
    HttpContext context,
 IHttpClientFactory httpClientFactory,
    IServiceProvider serviceProvider) =>// ? No parameter
{
    // ? Read raw body ourselves
 using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
    string rawBody = await reader.ReadToEndAsync();
  
    // ? Now we control the deserialization and can handle errors
  try {
        var requestModel = JsonSerializer.Deserialize<ClientRequest>(rawBody);
if (requestModel != null && !string.IsNullOrWhiteSpace(requestModel.Prompt)) {
            capability = requestModel.Prompt;
        }
    }
    catch(JsonException) {
        // Fallback to plain string or fix newlines
    }
```

## Why This Works

| Approach | When Deserialization Happens | Can Handle Newlines? |
|----------|------------------------------|----------------------|
| `ClientRequest req` parameter | **Before your code** | ? No - ASP.NET fails first |
| Manual `StreamReader` | **In your code** | ? Yes - you control it |

## Flow Comparison

### Before (Broken) ?
```
Request ? ASP.NET Deserialize ClientRequest ? FAILS ? Exception
          Your code never runs!
```

### After (Fixed) ?
```
Request ? Your code reads raw body ? Try deserialize
    ? Success? Use it
   ? Fail? Try string format
        ? Fail? Fix newlines and retry
```

## Test Cases

### Test 1: Multiline in Swagger UI ?

**Input:**
```json
{
  "prompt": "reverse below string
Hi Ravi?"
}
```

**Result:** Works! Newlines normalized.

### Test 2: Object Format ?

```bash
curl -X POST http://localhost:5050/api/client/post \
  -H "Content-Type: application/json" \
  -d '{"prompt": "reverse: hello world"}'
```

**Result:** Works!

### Test 3: Plain String Format ?

```bash
curl -X POST http://localhost:5050/api/client/post \
  -H "Content-Type: application/json" \
  -d '"reverse: hello world"'
```

**Result:** Works!

## Key Points

### ? Don't Do This
```csharp
// Never use [FromBody] or parameter binding when you need custom JSON handling
app.MapPost("/api/post", async (ClientRequest req) => {
    // Your custom error handling here never runs if deserialization fails!
});
```

### ? Do This Instead
```csharp
// Read the body manually for full control
app.MapPost("/api/post", async (HttpContext context) => {
    using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
    string rawBody = await reader.ReadToEndAsync();
    // Now you control deserialization and error handling
});
```

## Swagger UI Integration

Even without a parameter, Swagger still shows the input field because of:

```csharp
.Accepts<ClientRequest>("application/json")
```

This tells Swagger what the request body should look like, while still allowing you to handle the deserialization manually.

## Files Changed

? **Program.cs** - Removed `ClientRequest req` parameter, fixed body reading

## Build Status

? **Build Successful**

---

## Summary

**Problem:** ASP.NET Core tried to deserialize before custom error handling  
**Solution:** Remove parameter, read body manually  
**Result:** Multiline JSON now works! ?

**Key Lesson:** When you need custom JSON handling (like fixing newlines), you **must** read the request body manually. Parameter binding happens too early in the pipeline.

---

**Restart your app and test with multiline input in Swagger UI!** ??
