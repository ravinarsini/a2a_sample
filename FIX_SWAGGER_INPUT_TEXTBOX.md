# Fix: Swagger UI Not Showing Input Text Box

## Problem

After switching from `[FromBody] string prompt` to manual body reading, Swagger UI stopped showing an input text box because it couldn't detect what the API expects.

## Solution

Created a **`ClientRequest` model** and updated the endpoint to accept it while maintaining backward compatibility.

### What Changed

#### 1. Created ClientRequest Model

```csharp
public class ClientRequest
{
    /// <summary>
    /// The user's natural language request or command
    /// </summary>
    /// <example>reverse below string Hi Ravi?</example>
    public string Prompt { get; set; } = string.Empty;
}
```

#### 2. Updated Endpoint

```csharp
// Accepts both formats now
.Accepts<ClientRequest>("application/json")

// In the handler:
try {
    var requestModel = JsonSerializer.Deserialize<ClientRequest>(rawBody);
    if (requestModel != null && !string.IsNullOrWhiteSpace(requestModel.Prompt))
    {
        capability = requestModel.Prompt;  // From object
    }
    else
    {
        capability = JsonSerializer.Deserialize<string>(rawBody);  // From string
    }
}
```

## How to Use

### Option 1: Swagger UI (Recommended)

1. Open `http://localhost:5050/swagger`
2. Expand `/api/client/post`
3. Click **"Try it out"**
4. You'll now see a text box with this schema:

```json
{
  "prompt": "string"
}
```

5. Enter your request:

```json
{
  "prompt": "reverse below string Hi Ravi?"
}
```

6. Click **Execute**

### Option 2: curl with Object Format

```bash
curl -X POST http://localhost:5050/api/client/post \
  -H "Content-Type: application/json" \
  -d '{"prompt": "reverse below string Hi Ravi?"}'
```

### Option 3: curl with Plain String (Still Works!)

```bash
curl -X POST http://localhost:5050/api/client/post \
  -H "Content-Type: application/json" \
  -d '"reverse below string Hi Ravi?"'
```

## Benefits

? **Swagger UI works** - Shows nice input field  
? **Backward compatible** - Plain string format still works  
? **Multiline support** - Still handles newlines gracefully  
? **Better documentation** - Schema visible in Swagger  

## Request Formats Supported

| Format | Example | Status |
|--------|---------|--------|
| **Object (New)** | `{"prompt": "reverse: hello"}` | ? Swagger UI |
| **Plain String** | `"reverse: hello"` | ? curl/API |
| **Multiline** | `{"prompt": "reverse\nhello"}` | ? Both |

## Testing

### Test in Swagger UI

1. Go to http://localhost:5050/swagger
2. Try this request:

```json
{
  "prompt": "reverse below string Hi Ravi?"
}
```

**Expected Response:**
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

### Test with curl (Object Format)

```bash
curl -X POST http://localhost:5050/api/client/post \
  -H "Content-Type: application/json" \
-d '{
    "prompt": "find news about AI"
  }'
```

### Test with curl (String Format - Still Works)

```bash
curl -X POST http://localhost:5050/api/client/post \
  -H "Content-Type: application/json" \
  -d '"find news about AI"'
```

## Files Changed

? **ClientRequest.cs** (NEW) - Request model for Swagger  
? **Program.cs** - Updated to accept both formats

## Build Status

? **Build Successful**

---

**Swagger UI now shows a proper input field! ??**
