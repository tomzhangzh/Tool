namespace SalesDataAnalyzer.Services.AI;

public interface ILLMService
{
    Task<string> GenerateResponseAsync(string userMessage, string context);
}

public class LLMServiceOptions
{
    public string Provider { get; set; } = "openai";
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-3.5-turbo";
}