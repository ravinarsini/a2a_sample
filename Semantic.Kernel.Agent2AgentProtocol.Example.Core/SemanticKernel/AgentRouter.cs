using Microsoft.SemanticKernel;
using System.ComponentModel;

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
    /// Extracts the parameter from a command string
    /// </summary>
    private string ExtractParameter(string request, string command)
    {
        // Try to extract after colon
 if (request.Contains(':'))
   {
            int colonIndex = request.IndexOf(':');
         return request.Substring(colonIndex + 1).Trim();
        }

// Try to extract after the command word
    int commandIndex = request.IndexOf(command, StringComparison.OrdinalIgnoreCase);
        if (commandIndex >= 0)
        {
            string afterCommand = request.Substring(commandIndex + command.Length).Trim();
       // Remove common words
     afterCommand = afterCommand.TrimStart(':', ' ', '-');
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
Input: ""make this UPPER: test"" ? uppercase_text|test";

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
