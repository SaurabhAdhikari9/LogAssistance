using System.Text.Json.Serialization;

namespace LogAssistance.WebApp.Services.LogTool;

public class LogEntryDto
{
    public string Timestamp { get; set; }
    public string Level { get; set; }
    public string Message { get; set; }
    public string Prop_SourceContext { get; set; }
    public string Prop_Application { get; set; }
}

public class FunctionDefDto
{
    public string Name { get; set; }
    public string description { get; set; }
    public object parameters { get; set; }
}
public class ToolDto
{
    public string Type { get; set; } = "function";
    public FunctionDefDto Function { get; set; }
}

public class ToolCallDto
{
    [JsonPropertyName("function")]
    public FunctionCallDto Function { get; set; }
}

public class FunctionCallDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("arguments")]
    public Dictionary<string, object> Arguments { get; set; }
}
