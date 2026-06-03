using System.Text.Json;
using System.Text.Json.Serialization;
using LogAssistance.WebApp.Services.LogTool;

namespace LogAssistance.WebApp.Services.Assistance;

public class AssistanceService(HttpClient httpClient, ILogService _logService) : IAssistanceService
{
    // 1. The List of tools we send to Ollama
    private readonly List<ToolDto> _availableTools = new();
    
    // 2. The Registry mapping tool names to actual C# execution logic
    private readonly Dictionary<string, Func<JsonElement, Task<string>>> _toolHandlers = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    
    private void RegisterGetRecentLogsTool()
    {
        // Add the definition for Ollama to read
        _availableTools.Add(new ToolDto
        {
            Type = "function",
            Function = new FunctionDefDto
            {
                Name = "get_recent_logs",
                description = "Gets the application logs from the server for the specified number of minutes in the past.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        minutesAgo = new { type = "integer", description = "The number of minutes in the past to search." }
                    },
                    required = new[] { "minutesAgo" }
                }
            }
        });

        // Map the execution logic
        _toolHandlers["get_recent_logs"] = async (arguments) =>
        {
            if (arguments.TryGetProperty("minutesAgo", out var minutesObj) && minutesObj.TryGetInt32(out int minutes))
            {
                var logs = (await _logService.GetRecentLogsAsync(minutes)).ToList();
                return JsonSerializer.Serialize(logs, _jsonOptions);
            }
            return "Error: Invalid or missing 'minutesAgo' argument.";
        };
    }
    
    private readonly ToolDto _getLogsTool = new ToolDto
    {
        Type = "function",
        Function = new FunctionDefDto
        {
            Name = "get_recent_logs",
            description = "Gets the application logs from the server for the specified number of minutes in the past.",
            parameters = new
            {
                type = "object",
                properties = new
                {
                    minutesAgo = new
                    {
                        type = "integer",
                        description = "The number of minutes in the past to search. E.g., 15 for 'last 15 minutes'."
                    }
                },
                required = new[] { "minutesAgo" }
            }
        }
    };
    public async Task<string> SendMessageAsync(OllamaRequestDto request)
    {
        try
        {
            RegisterGetRecentLogsTool();
        request.Tools =_availableTools;
        
        // Ensure request.Stream is explicitly false!
        request.Stream = false; 

        var response = await httpClient.PostAsJsonAsync("api/chat", request, _jsonOptions);
        
        if (response.IsSuccessStatusCode)
        {
            // ==========================================
            // THE INTERCEPTOR: Read the raw text first!
            // ==========================================
            string rawJson = await response.Content.ReadAsStringAsync();
            
            // Log this to your JetBrains Rider console so we can see the absolute truth
            Console.WriteLine("\n=== RAW OLLAMA RESPONSE ===");
            Console.WriteLine(rawJson);
            Console.WriteLine("===========================\n");

            // Now deserialize the raw string manually using your _jsonOptions
            var result = JsonSerializer.Deserialize<OllamaResponseDto>(rawJson, _jsonOptions);
            var aiMessage = result?.Message;

            if (aiMessage == null) return "Error: Message content was null.";

            if (aiMessage.Tool_Calls != null && aiMessage.Tool_Calls.Any())
            {
                var toolCall = aiMessage.Tool_Calls.FirstOrDefault()?.Function;
                if (toolCall == null) return "Error: Tool function was null.";
        
                if (_toolHandlers.TryGetValue(toolCall.Name, out var handler))
                {
                    var minutesArgs = toolCall.Arguments["minutesAgo"].ToString();
                    if (int.TryParse(minutesArgs, out int minutes))
                    {
                        // Limit logs to 5 so we don't blow up Llama's memory window
                        var logs = (await _logService.GetRecentLogsAsync(minutes)).ToList();
                        
                        // FIX: Pass _jsonOptions to the serializer!
                        string serializedLogs = JsonSerializer.Serialize(logs, _jsonOptions);

                        request.Messages.Add(aiMessage); 
                        request.Messages.Add(new ChatMessage() 
                        { 
                            Role = "tool", 
                            Content = serializedLogs,
                            Name = toolCall.Name // FIX: Ollama requires the Name here!
                        });

                        request.Tools = null; 

                        var finalResponse = await httpClient.PostAsJsonAsync("api/chat", request, _jsonOptions);
                        
                        // Repeat the interceptor for the final call
                        string finalRawJson = await finalResponse.Content.ReadAsStringAsync();
                        var finalResult = JsonSerializer.Deserialize<OllamaResponseDto>(finalRawJson, _jsonOptions);
                
                        return finalResult?.Message?.Content ?? "No summary provided.";
                    }
                }
            }
            return aiMessage.Content ?? "No response received!";
        }
        
        return $"Error: {response.StatusCode} - {response.ReasonPhrase}";
    }
    catch (Exception ex)
    {
        return $"Error: {ex.Message}";
    }
    }
}