using System.ClientModel;
using System.Globalization;
using Microsoft.Extensions.Logging;
using OpenAi.Models.Exceptions;
using OpenAi.Repository.Interfaces;
using OpenAI.Chat;

namespace OpenAi.Repository.Implementations;

public class AzureOpenAIChatRepository : IChatCompletionRepository
{
    private static readonly ChatCompletionOptions RequestOptions = new()
    {
        Temperature = 1.0f,
        TopP = 1.0f,
        FrequencyPenalty = 0.0f,
        PresencePenalty = 0.0f,
    };

    private readonly ChatClient _chatClient;
    private readonly ILogger<AzureOpenAIChatRepository> _logger;

    public AzureOpenAIChatRepository(ChatClient chatClient, ILogger<AzureOpenAIChatRepository> logger)
    {
        _chatClient = chatClient;
        _logger = logger;
    }

    public async Task<string?> CompleteAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        List<ChatMessage> messages = [new UserChatMessage(userMessage)];

        ChatCompletion completion;
        try
        {
            completion = (await _chatClient.CompleteChatAsync(messages, RequestOptions, cancellationToken)).Value;
        }
        catch (ClientResultException ex)
        {
            int? status = ex.Status == 0 ? null : ex.Status;
            throw new ExternalServiceException(
                $"Azure OpenAI chat completion failed with status {status?.ToString(CultureInfo.InvariantCulture) ?? "none"}.",
                status,
                GetRetryAfter(ex),
                ex);
        }

        // Log metadata only: prompts and replies may contain personal data.
        _logger.LogInformation(
            "Chat completion finished. FinishReason={FinishReason}, InputTokens={InputTokens}, OutputTokens={OutputTokens}",
            completion.FinishReason,
            completion.Usage?.InputTokenCount,
            completion.Usage?.OutputTokenCount);

        return completion.Content.Count > 0 ? completion.Content[0].Text : null;
    }

    private static TimeSpan? GetRetryAfter(ClientResultException ex)
    {
        if (ex.GetRawResponse()?.Headers.TryGetValue("Retry-After", out string? value) == true
            && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int seconds)
            && seconds >= 0)
        {
            return TimeSpan.FromSeconds(seconds);
        }

        return null;
    }
}
