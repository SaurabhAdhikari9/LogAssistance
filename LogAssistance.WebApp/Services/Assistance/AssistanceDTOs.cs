using System.Text.Json.Serialization;
using LogAssistance.WebApp.Services.LogTool;

namespace LogAssistance.WebApp.Services.Assistance;

public class OllamaRequestDto
{
    public readonly string AssistantName = "Hero";

    public OllamaRequestDto(string prompt)
    {
        Messages.Add(new ChatMessage()
        {
            Role = "System",
            Content = "You are a highly capable AI assistant named Saurabh. " +
                      "You are an expert in software engineering, cloud infrastructure, and application logs. " +
                      $"Always stay in character as {AssistantName}. " +
                      $"If asked who you are, proudly introduce yourself as {AssistantName}." +
                      " NEVER show raw JSON tool calls to the user. " +
                      "NEVER make up or simulate log data. Only use the data provided by the tool. " +
                      "When presenting logs, ALWAYS format them as a clean Markdown table with these columns: Timestamp | Level | Message." +
                      "Do not add unnecessary apologies or conversational filler."
        });
        Messages.Add(new ChatMessage()
        {
            Role = "User",
            Content = prompt
        });
    }

    public string Model { get; set; } = "llama3.2";
    

    // System property:
    public string System { get; set; }

    public bool Stream { get; set; } = false;
    public List<ToolDto> Tools { get; set; } = new();
    public List<ChatMessage> Messages { get; set; } = new();
}

public class OllamaResponseDto()
{
    [JsonPropertyName("message")]
    public ChatMessage Message { get; set; }
}

public class ChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } 
    [JsonPropertyName("content")]
    public string Content { get; set; }
    [JsonPropertyName("tool_calls")]
    public List<ToolCallDto>? Tool_Calls { get; set; }
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
