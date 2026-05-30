namespace LogAssistance.WebApp.Services.LogTool;

public interface ILogService
{
    Task<List<LogEntryDto>> GetRecentLogsAsync(int minutesAgo);
}
