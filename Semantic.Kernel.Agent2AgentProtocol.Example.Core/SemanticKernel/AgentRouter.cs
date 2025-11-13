using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace Semantic.Kernel.Agent2AgentProtocol.Example.Core.SemanticKernel;

/// <summary>
/// Intelligent router that uses Semantic Kernel to determine which agent function to invoke
/// based on natural language input
/// </summary>
public class AgentRouter
{
  private readonly Microsoft.SemanticKernel.Kernel _kernel;

    public AgentRouter(Microsoft.SemanticKernel.Kernel kernel)
    {
  _kernel = kernel;
    }

    /// <summary>
    /// Intelligently routes a user request to the appropriate agent function
  /// </summary>
    /// <param name="userRequest">The natural language request from the user</param>
    /// <returns>The result of the function execution</returns>
    public async Task<FunctionResult> RouteAndExecuteAsync(string userRequest)
    {
  // Create a prompt that helps the kernel understand intent and route to the right function
 string routingPrompt = $@"Analyze the following user request and determine what action to take:

User Request: ""{userRequest}""

Available capabilities:
- reverse_text: Reverses any text input
- uppercase_text: Converts text to uppercase
- search_news: Searches for news about a specific topic

Based on the request, identify:
1. The appropriate function to call
2. The input parameter to pass to that function

If the request is about reversing text, use reverse_text.
If the request is about converting to uppercase or capitalizing, use uppercase_text.
If the request is about news, current events, or searching for information, use search_news.

Extract the relevant text or topic from the request.

Return in format: FUNCTION_NAME|PARAMETER
Example: reverse_text|hello world
Example: search_news|artificial intelligence";

  try
  {
// Get the routing decision from the kernel
     FunctionResult routingResult = await _kernel.InvokePromptAsync(routingPrompt);
 string routingDecision = routingResult.ToString().Trim();

    // Parse the routing decision
string[] parts = routingDecision.Split('|', 2);
          if (parts.Length != 2)
   {
      throw new InvalidOperationException($"Invalid routing decision: {routingDecision}");
  }

  string functionName = parts[0].Trim();
   string parameter = parts[1].Trim();

// Execute the determined function
 KernelPlugin plugin = _kernel.Plugins["TextProcessing"];
            KernelFunction function = plugin[functionName];

   KernelArguments arguments = new() { ["input"] = parameter, ["topic"] = parameter };
 
      return await _kernel.InvokeAsync(function, arguments);
    }
        catch (Exception ex)
      {
  // Fallback: try to manually parse the request
   return await FallbackRouting(userRequest);
   }
    }

    /// <summary>
    /// Simple heuristic-based routing as fallback
    /// </summary>
    private async Task<FunctionResult> FallbackRouting(string userRequest)
    {
      string lowerRequest = userRequest.ToLowerInvariant();
     
    // Extract command and parameter
        string command = "";
        string parameter = "";

        if (lowerRequest.Contains("reverse") || lowerRequest.Contains(":") && lowerRequest.StartsWith("reverse"))
  {
command = "reverse_text";
   parameter = ExtractParameter(userRequest, "reverse");
     }
  else if (lowerRequest.Contains("uppercase") || lowerRequest.Contains("upper") || 
     (lowerRequest.Contains(":") && lowerRequest.StartsWith("upper")))
    {
   command = "uppercase_text";
  parameter = ExtractParameter(userRequest, "upper");
    }
    else if (lowerRequest.Contains("news") || lowerRequest.Contains("search"))
    {
  command = "search_news";
  parameter = ExtractParameter(userRequest, "news");
      }
  else
      {
    throw new InvalidOperationException($"Unable to determine action for request: {userRequest}");
}

      KernelPlugin plugin = _kernel.Plugins["TextProcessing"];
     KernelFunction function = plugin[command];
   
        KernelArguments arguments = new() { ["input"] = parameter, ["topic"] = parameter };
   
     return await _kernel.InvokeAsync(function, arguments);
}

    /// <summary>
    /// Extracts the parameter from a command string using improved pattern matching
    /// </summary>
    private string ExtractParameter(string request, string command)
    {
        // Try to extract after colon first (standard format like "reverse: text")
        if (request.Contains(':'))
      {
   int colonIndex = request.IndexOf(':');
            return request.Substring(colonIndex + 1).Trim();
        }

        // Try to extract quoted text (e.g., "reverse 'text'" or "reverse \"text\"")
 var singleQuoteMatch = Regex.Match(request, @"'([^']+)'");
        if (singleQuoteMatch.Success)
        {
            return singleQuoteMatch.Groups[1].Value;
        }

   var doubleQuoteMatch = Regex.Match(request, @"""([^""]+)""");
    if (doubleQuoteMatch.Success)
        {
            return doubleQuoteMatch.Groups[1].Value;
        }

        // Try to extract after common patterns
    // Patterns: "reverse string X", "reverse text X", "reverse the text X", etc.
        string[] patterns = new[]
  {
    $@"{command}\s+(?:string|text|the\s+(?:string|text|word|phrase))\s+['""]?([^'""]+)['""]?",
          $@"{command}\s+(?:this|the)?\s*['""]?([^'""]+)['""]?",
       $@"{command}\s+(.+)"
      };

        foreach (string pattern in patterns)
        {
            var match = Regex.Match(request, pattern, RegexOptions.IgnoreCase);
      if (match.Success && match.Groups.Count > 1)
{
         string extracted = match.Groups[1].Value.Trim();
    // Clean up common trailing words
      extracted = Regex.Replace(extracted, @"\s*(?:please|now|thanks?)\s*$", "", RegexOptions.IgnoreCase).Trim();
         if (!string.IsNullOrWhiteSpace(extracted))
         {
   return extracted;
            }
         }
        }

        // Fallback: extract after the command word
        int commandIndex = request.IndexOf(command, StringComparison.OrdinalIgnoreCase);
        if (commandIndex >= 0)
        {
            string afterCommand = request.Substring(commandIndex + command.Length).Trim();
            // Remove common words
            afterCommand = afterCommand.TrimStart(':', ' ', '-');
    // Remove leading filler words
afterCommand = Regex.Replace(afterCommand, @"^(?:the|this|string|text|word|phrase)\s+", "", RegexOptions.IgnoreCase).Trim();
            return afterCommand;
        }

 return request;
    }

    /// <summary>
    /// Uses AI to intelligently determine intent and extract parameters
    /// </summary>
    public async Task<(string functionName, string parameter)> DetermineIntentAsync(string userRequest)
    {
   string intentPrompt = $@"Analyze this user request and extract the intent and parameter:

Request: ""{userRequest}""

Return ONLY in this format (no extra text):
FUNCTION|PARAMETER

Functions available:
- reverse_text (for reversing text)
- uppercase_text (for converting to uppercase)
- search_news (for news searches)

Examples:
Input: ""reverse: hello world"" ? reverse_text|hello world
Input: ""find news about AI"" ? search_news|AI
Input: ""make this UPPER: test"" ? uppercase_text|test
Input: ""reverse string 'Hi Ravi?'"" ? reverse_text|Hi Ravi?";

        FunctionResult result = await _kernel.InvokePromptAsync(intentPrompt);
        string response = result.ToString().Trim();

        string[] parts = response.Split('|', 2);
        if (parts.Length == 2)
        {
        return (parts[0].Trim(), parts[1].Trim());
        }

        throw new InvalidOperationException($"Could not determine intent from: {userRequest}");
    }
}
