using OpenAi.Models.Exceptions;

namespace OpenAi.Repository.Interfaces;

public interface IChatCompletionRepository
{
    Task<string?> CompleteAsync(string userMessage, CancellationToken cancellationToken = default);
}
