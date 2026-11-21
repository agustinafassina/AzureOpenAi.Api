using System.ComponentModel.DataAnnotations;
using OpenAi.Models.Settings;

namespace OpenAi.Api.Tests.Settings;

public class OpenAISettingTests
{
    private static List<ValidationResult> Validate(OpenAISetting settings)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(settings, new ValidationContext(settings), results, validateAllProperties: true);
        return results;
    }

    private static OpenAISetting ValidSettings() => new()
    {
        Endpoint = "https://test.openai.azure.com/",
        DeploymentName = "deployment",
        ApiKey = "api-key",
    };

    [Fact]
    public void Validate_AllValuesPresent_IsValid()
    {
        Assert.Empty(Validate(ValidSettings()));
    }

    [Theory]
    [InlineData(nameof(OpenAISetting.Endpoint))]
    [InlineData(nameof(OpenAISetting.DeploymentName))]
    [InlineData(nameof(OpenAISetting.ApiKey))]
    public void Validate_MissingValue_IsInvalid(string property)
    {
        OpenAISetting settings = ValidSettings();
        typeof(OpenAISetting).GetProperty(property)!.SetValue(settings, "");

        ValidationResult error = Assert.Single(Validate(settings));
        Assert.Contains(property, error.MemberNames);
    }

    [Fact]
    public void Validate_EndpointIsNotAUrl_IsInvalid()
    {
        OpenAISetting settings = ValidSettings();
        settings.Endpoint = "not-a-url";

        ValidationResult error = Assert.Single(Validate(settings));
        Assert.Contains(nameof(OpenAISetting.Endpoint), error.MemberNames);
    }
}
