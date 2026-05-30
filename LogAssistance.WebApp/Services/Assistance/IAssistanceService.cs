namespace LogAssistance.WebApp.Services.Assistance;

public interface IAssistanceService
{
    Task<string> SendMessageAsync(OllamaRequestDto request);
}