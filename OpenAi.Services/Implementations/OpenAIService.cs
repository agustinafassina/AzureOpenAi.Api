using Microsoft.Extensions.Logging;
using OpenAi.Repository.Interfaces;
using OpenAi.Services.Interfaces;

namespace OpenAi.Services.Implementations;

public class OpenAIService : IOpenAIService
{
    private readonly IChatCompletionRepository _chatRepository;
    private readonly ILogger<OpenAIService> _logger;

    public OpenAIService(IChatCompletionRepository chatRepository, ILogger<OpenAIService> logger)
    {
        _chatRepository = chatRepository;
        _logger = logger;
    }

    public async Task<string?> AskAsync(string question, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(question);

        string? reply = await _chatRepository.CompleteAsync(question, cancellationToken);

        if (string.IsNullOrWhiteSpace(reply))
        {
            _logger.LogWarning("Chat model returned an empty reply");
            return null;
        }

        return reply;
    }
}
