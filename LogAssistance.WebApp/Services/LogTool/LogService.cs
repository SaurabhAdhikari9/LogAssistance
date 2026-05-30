using System.Globalization;
using System.Text.Json;

namespace LogAssistance.WebApp.Services.LogTool;

public class LogService : ILogService
{
    private readonly string _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

    public async Task<List<LogEntryDto>> GetRecentLogsAsync(int minutesAgo)
    {
        var matchedLogs = new List<LogEntryDto>();

        // 1. Calculate our cutoff time
        var cutoffTime = DateTime.Now.AddMinutes(-minutesAgo);

        // 2. Locate today's active log file based on Serilog's default naming (app-yyyyMMdd.json)
        string todaySuffix = DateTime.Now.ToString("yyyyMMdd");
        string filePath = Path.Combine(_logDirectory, $"app-{todaySuffix}.json");

        if (!File.Exists(filePath))
        {
            return matchedLogs; // Return empty list if no logs have been written yet today
        }

        // 3. Read the file line-by-line (JSON Lines format) using an async stream reader
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(fs);

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var entry = JsonSerializer.Deserialize<LogEntryDto>(line, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (entry != null && !string.IsNullOrEmpty(entry.Timestamp))
                {
                    // 4. Parse the timestamp string back into a C# DateTime to check our window
                    if (DateTime.TryParseExact(entry.Timestamp, "yyyy-MM-dd HH:mm:ss",
                            CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime logTime))
                    {
                        if (logTime >= cutoffTime)
                        {
                            matchedLogs.Add(entry);
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Single line corruption shouldn't stop the tool from reading the rest of the logs
                continue;
            }
        }

        // Return the logs sorted with the newest entries first
        return matchedLogs.OrderByDescending(l => l.Timestamp).ToList();
    }
}