using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using OpenAi.Models.Exceptions;
using OpenAi.Repository.Implementations;
using OpenAI.Chat;

namespace OpenAi.Api.Tests.Repository;

public class AzureOpenAIChatRepositoryTests
{
    private readonly Mock<ChatClient> _chatClientMock = new();

    private AzureOpenAIChatRepository CreateSut() =>
        new(_chatClientMock.Object, NullLogger<AzureOpenAIChatRepository>.Instance);

    private void SetupCompletion(ChatCompletion completion) =>
        _chatClientMock
            .Setup(c => c.CompleteChatAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatCompletionOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ClientResult.FromValue(completion, Mock.Of<PipelineResponse>()));

    private void SetupFailure(int status, string? retryAfter = null)
    {
        var headers = new Mock<PipelineResponseHeaders>();
        string? headerValue = retryAfter;
        headers.Setup(h => h.TryGetValue("Retry-After", out headerValue)).Returns(retryAfter is not null);

        var response = new Mock<PipelineResponse>();
        response.SetupGet(r => r.Status).Returns(status);
        // PipelineResponse.Headers is not virtual; it returns the protected HeadersCore.
        response.Protected().SetupGet<PipelineResponseHeaders>("HeadersCore").Returns(headers.Object);

        _chatClientMock
            .Setup(c => c.CompleteChatAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatCompletionOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ClientResultException(response.Object));
    }

    [Fact]
    public async Task CompleteAsync_ModelReturnsText_ReturnsFirstContentPart()
    {
        SetupCompletion(OpenAIChatModelFactory.ChatCompletion(content: new ChatMessageContent("Hi there!")));

        string? result = await CreateSut().CompleteAsync("Hello");

        Assert.Equal("Hi there!", result);
    }

    [Fact]
    public async Task CompleteAsync_ModelReturnsNoContent_ReturnsNull()
    {
        SetupCompletion(OpenAIChatModelFactory.ChatCompletion(content: new ChatMessageContent()));

        string? result = await CreateSut().CompleteAsync("Hello");

        Assert.Null(result);
    }

    [Fact]
    public async Task CompleteAsync_SendsUserMessageAndCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        SetupCompletion(OpenAIChatModelFactory.ChatCompletion(content: new ChatMessageContent("ok")));

        await CreateSut().CompleteAsync("Hello", cts.Token);

        _chatClientMock.Verify(c => c.CompleteChatAsync(
            It.Is<IEnumerable<ChatMessage>>(m => m.Single() is UserChatMessage && m.Single().Content[0].Text == "Hello"),
            It.IsAny<ChatCompletionOptions>(),
            cts.Token), Times.Once);
    }

    [Fact]
    public async Task CompleteAsync_Throttled_ThrowsTransientExternalServiceExceptionWithRetryAfter()
    {
        SetupFailure(429, retryAfter: "12");

        ExternalServiceException ex = await Assert.ThrowsAsync<ExternalServiceException>(() => CreateSut().CompleteAsync("Hello"));

        Assert.Equal(429, ex.UpstreamStatusCode);
        Assert.True(ex.IsTransient);
        Assert.Equal(TimeSpan.FromSeconds(12), ex.RetryAfter);
        Assert.IsType<ClientResultException>(ex.InnerException);
    }

    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(404)]
    public async Task CompleteAsync_ClientError_ThrowsNonTransientExternalServiceException(int status)
    {
        SetupFailure(status);

        ExternalServiceException ex = await Assert.ThrowsAsync<ExternalServiceException>(() => CreateSut().CompleteAsync("Hello"));

        Assert.Equal(status, ex.UpstreamStatusCode);
        Assert.False(ex.IsTransient);
        Assert.Null(ex.RetryAfter);
    }

    [Fact]
    public async Task CompleteAsync_InvalidRetryAfterHeader_IgnoresIt()
    {
        SetupFailure(429, retryAfter: "not-a-number");

        ExternalServiceException ex = await Assert.ThrowsAsync<ExternalServiceException>(() => CreateSut().CompleteAsync("Hello"));

        Assert.Null(ex.RetryAfter);
    }
}
