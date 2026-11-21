namespace OpenAi.Services.Interfaces;

public interface IOpenAIService
{
    Task<string?> AskAsync(string question, CancellationToken cancellationToken = default);
}