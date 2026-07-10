using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OpenAi.Api.Controllers;
using OpenAi.Api.Controllers.Contracts.Requests;
using OpenAi.Api.Controllers.Contracts.Response;
using OpenAi.Api.Services.Interfaces;

namespace OpenAi.Api.Tests.Controllers;

public class ChatControllerTests
{
    private readonly Mock<IOpenAIService> _openAiServiceMock = new();
    private readonly Mock<IMapper> _mapperMock = new();

    private ChatController CreateController() =>
        new(_openAiServiceMock.Object, _mapperMock.Object);

    [Fact]
    public void GetChatgpt_ReturnsOkWithReply_WhenServiceReturnsResponse()
    {
        const string message = "Hello";
        const string reply = "Hi there!";

        _openAiServiceMock
            .Setup(service => service.GetQuestion(message))
            .Returns(reply);

        IActionResult? result = CreateController().GetChatgpt(new QuestionRequest { Message = message });

        OkObjectResult? okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        ChatResponse? response = okResult.Value.Should().BeOfType<ChatResponse>().Subject;
        response.Reply.Should().Be(reply);
        _openAiServiceMock.Verify(service => service.GetQuestion(message), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetChatgpt_ReturnsNotFound_WhenServiceReturnsNullOrEmpty(string? serviceResponse)
    {
        const string message = "Hello";

        _openAiServiceMock
            .Setup(service => service.GetQuestion(message))
            .Returns(serviceResponse!);

        IActionResult? result = CreateController().GetChatgpt(new QuestionRequest { Message = message });

        NotFoundObjectResult? notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().Be("No response from OpenAI service.");
    }
}
