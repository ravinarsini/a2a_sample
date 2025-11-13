using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Semantic.Kernel.Agent2AgentProtocol.Example.Core.SemanticKernel;

/// <summary>
/// Plugin containing text processing and AI-powered functions that can be invoked by Semantic Kernel
/// </summary>
public class TextProcessingPlugin
{
    [KernelFunction("reverse_text")]
    [Description("Reverses the input text")]
    public string Reverse(
     [Description("The text to reverse")] string input)
    {
        return new string(input.Reverse().ToArray());
 }

 [KernelFunction("uppercase_text")]
    [Description("Converts the input text to uppercase")]
    public string Uppercase(
        [Description("The text to convert to uppercase")] string input)
    {
        return input.ToUpperInvariant();
    }

    [KernelFunction("search_news")]
    [Description("Searches for news about a topic using AI and returns a formatted summary")]
    public async Task<string> SearchNews(
        [Description("The news topic to search for")] string topic,
  Microsoft.SemanticKernel.Kernel kernel)
    {
 try
        {
    // Use ChatGPT to generate news summary
         string prompt = $@"Search for the latest news about: {topic}
            
Please provide a concise summary of recent news articles about this topic.
Format the response as:
- Date and headline
- Brief description
- Source (if available)

Provide 3-5 recent news items.";

      FunctionResult result = await kernel.InvokePromptAsync(prompt);
     return result.ToString();
        }
    catch (Exception ex)
        {
       return $"Error searching for news: {ex.Message}";
        }
    }
}

/// <summary>
/// Legacy factory for backward compatibility - will be deprecated
/// </summary>
[Obsolete("Use TextProcessingPlugin with kernel.ImportPluginFromType instead")]
public static class TextProcessingFunction
{
    public static KernelFunction GetFunctionByType(string type)
    {
        return type.ToUpperInvariant() switch
  {
  "REVERSE" => KernelFunctionFactory.CreateFromMethod(
                (string input) => new string(input.Reverse().ToArray()), 
         "Reverse", 
 "Reverses text"),
       "UPPER" => KernelFunctionFactory.CreateFromMethod(
                (string input) => input.ToUpperInvariant(), 
        "Uppercase", 
      "Converts to uppercase"),
      "NEWS" => KernelFunctionFactory.CreateFromMethod(
     async (string input, Microsoft.SemanticKernel.Kernel kernel) =>
                {
            var plugin = new TextProcessingPlugin();
         return await plugin.SearchNews(input, kernel);
  }, 
          "SearchNews", 
    "Searches for news"),
            _ => throw new InvalidOperationException($"Unknown function type: {type}")
     };
    }
}