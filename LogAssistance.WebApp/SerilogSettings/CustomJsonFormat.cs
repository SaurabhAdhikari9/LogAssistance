using System.Text.Json;
using Serilog.Events;
using Serilog.Formatting;

namespace LogAssistance.WebApp.SerilogSettings;

public class CustomJsonFormat: ITextFormatter
{
    public void Format(LogEvent logEvent, TextWriter output)
    {
        var logPayload = new Dictionary<string, object>
        {
            { "timestamp", logEvent.Timestamp.ToString("yyyy-MM-dd HH:mm:ss") },
            { "level", logEvent.Level.ToString() },
            { "message", logEvent.RenderMessage() }
        };

        // If an exception exists, add it to the JSON root cleanly
        if (logEvent.Exception != null)
        {
            logPayload.Add("exception_type", logEvent.Exception.GetType().Name);
            logPayload.Add("exception_message", logEvent.Exception.Message);
        }

        // Flatten all custom properties passed during structured logging (like Username, StatusCode, etc.)
        foreach (var property in logEvent.Properties)
        {
            // Clean up Serilog quotes from strings
            var value = property.Value.ToString().Trim('"');
            logPayload.Add($"prop_{property.Key.ToLower()}", value);
        }

        // Serialize the clean object to a single line of text
        var jsonString = JsonSerializer.Serialize(logPayload);
        output.WriteLine(jsonString);
    }
}