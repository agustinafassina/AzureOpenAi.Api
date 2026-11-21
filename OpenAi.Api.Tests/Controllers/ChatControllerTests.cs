using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OpenAi.Api.Contracts.Requests;
using OpenAi.Api.Contracts.Responses;
using OpenAi.Api.Controllers;
using OpenAi.Api.Validators;
using OpenAi.Services.Interfaces;

namespace OpenAi.Api.Tests.Controllers;

public class ChatControllerTests
{
    private readonly Mock<IOpenAIService> _openAiServiceMock = new();

    private ChatController CreateSut() =>
        new(_openAiServiceMock.Object, new QuestionRequestValidator(), NullLogger<ChatController>.Instance);

    [Fact]
    public async Task AskAsync_ServiceReturnsReply_ReturnsOkWithReply()
    {
        _openAiServiceMock
            .Setup(s => s.AskAsync("Hello", It.IsAny<CancellationToken>()))
            .ReturnsAsync("Hi there!");

        IActionResult result = await CreateSut().AskAsync(new QuestionRequest { Message = "Hello" }, CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        ChatResponse response = Assert.IsType<ChatResponse>(ok.Value);
        Assert.Equal("Hi there!", response.Reply);
    }

    [Fact]
    public async Task AskAsync_ServiceReturnsNull_Returns502Problem()
    {
        _openAiServiceMock
            .Setup(s => s.AskAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        IActionResult result = await CreateSut().AskAsync(new QuestionRequest { Message = "Hello" }, CancellationToken.None);

        ObjectResult objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status502BadGateway, objectResult.StatusCode);
        Assert.IsType<ProblemDetails>(objectResult.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AskAsync_BlankMessage_ReturnsValidationProblemWithoutCallingService(string message)
    {
        IActionResult result = await CreateSut().AskAsync(new QuestionRequest { Message = message }, CancellationToken.None);

        ObjectResult objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        ValidationProblemDetails problem = Assert.IsType<ValidationProblemDetails>(objectResult.Value);
        Assert.Contains(nameof(QuestionRequest.Message), problem.Errors.Keys);
        _openAiServiceMock.Verify(s => s.AskAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AskAsync_WithCancellationToken_PassesTokenToService()
    {
        using var cts = new CancellationTokenSource();
        _openAiServiceMock
            .Setup(s => s.AskAsync("Hello", cts.Token))
            .ReturnsAsync("Hi");

        await CreateSut().AskAsync(new QuestionRequest { Message = "Hello" }, cts.Token);

        _openAiServiceMock.Verify(s => s.AskAsync("Hello", cts.Token), Times.Once);
    }
}
