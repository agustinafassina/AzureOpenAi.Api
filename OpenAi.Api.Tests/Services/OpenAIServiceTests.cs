using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OpenAi.Models.Exceptions;
using OpenAi.Repository.Interfaces;
using OpenAi.Services.Implementations;

namespace OpenAi.Api.Tests.Services;

public class OpenAIServiceTests
{
    private readonly Mock<IChatCompletionRepository> _repositoryMock = new();

    private OpenAIService CreateSut() => new(_repositoryMock.Object, NullLogger<OpenAIService>.Instance);

    [Fact]
    public async Task AskAsync_RepositoryReturnsReply_ReturnsReply()
    {
        _repositoryMock
            .Setup(r => r.CompleteAsync("Hello", It.IsAny<CancellationToken>()))
            .ReturnsAsync("Hi there!");

        string? result = await CreateSut().AskAsync("Hello");

        Assert.Equal("Hi there!", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AskAsync_RepositoryReturnsNoText_ReturnsNull(string? repositoryReply)
    {
        _repositoryMock
            .Setup(r => r.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(repositoryReply);

        string? result = await CreateSut().AskAsync("Hello");

        Assert.Null(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AskAsync_BlankQuestion_ThrowsArgumentExceptionWithoutCallingRepository(string question)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => CreateSut().AskAsync(question));

        _repositoryMock.Verify(r => r.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AskAsync_WithCancellationToken_PassesTokenToRepository()
    {
        using var cts = new CancellationTokenSource();
        _repositoryMock
            .Setup(r => r.CompleteAsync("Hello", cts.Token))
            .ReturnsAsync("Hi");

        await CreateSut().AskAsync("Hello", cts.Token);

        _repositoryMock.Verify(r => r.CompleteAsync("Hello", cts.Token), Times.Once);
    }

    [Fact]
    public async Task AskAsync_RepositoryThrowsExternalServiceException_Propagates()
    {
        var failure = new ExternalServiceException("boom", 429);
        _repositoryMock
            .Setup(r => r.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(failure);

        ExternalServiceException thrown = await Assert.ThrowsAsync<ExternalServiceException>(() => CreateSut().AskAsync("Hello"));

        Assert.Same(failure, thrown);
    }
}
