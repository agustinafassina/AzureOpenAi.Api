using FluentAssertions;
using Microsoft.Extensions.Options;
using OpenAi.Api.Services.Entities;
using OpenAi.Api.Services.Implementations;

namespace OpenAi.Api.Tests.Services;

public class OpenAIServiceTests
{
    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenSettingsValueIsNull()
    {
        IOptions<OpenAISetting>? options = Options.Create<OpenAISetting>(null!);

        Func<OpenAIService>? act = () => new OpenAIService(options);

        act.Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("openAiSetting");
    }

    [Fact]
    public void GetQuestion_ThrowsUriFormatException_WhenEndpointIsEmpty()
    {
        OpenAIService? service = new OpenAIService(Options.Create(new OpenAISetting
        {
            Endpoint = "",
            DeploymentName = "deployment",
            ApiKey = "api-key"
        }));

        Func<string>? act = () => service.GetQuestion("test question");

        act.Should().Throw<UriFormatException>();
    }

    [Theory]
    [InlineData("https://test.openai.azure.com", "", "api-key")]
    [InlineData("https://test.openai.azure.com", "deployment", "")]
    public void GetQuestion_ThrowsArgumentException_WhenSettingsAreIncomplete(
        string endpoint,
        string deploymentName,
        string apiKey)
    {
        OpenAIService? service = new OpenAIService(Options.Create(new OpenAISetting
        {
            Endpoint = endpoint,
            DeploymentName = deploymentName,
            ApiKey = apiKey
        }));

        var act = () => service.GetQuestion("test question");

        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("OpenAI settings are not properly configured.");
    }
}
