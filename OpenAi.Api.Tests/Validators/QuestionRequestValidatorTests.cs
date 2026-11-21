using FluentValidation.Results;
using OpenAi.Api.Contracts.Requests;
using OpenAi.Api.Validators;

namespace OpenAi.Api.Tests.Validators;

public class QuestionRequestValidatorTests
{
    private readonly QuestionRequestValidator _sut = new();

    [Fact]
    public void Validate_ValidMessage_IsValid()
    {
        ValidationResult result = _sut.Validate(new QuestionRequest { Message = "Hello" });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_MessageAtMaxLength_IsValid()
    {
        var request = new QuestionRequest { Message = new string('a', QuestionRequestValidator.MaxMessageLength) };

        Assert.True(_sut.Validate(request).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_BlankMessage_IsInvalid(string message)
    {
        ValidationResult result = _sut.Validate(new QuestionRequest { Message = message });

        ValidationFailure error = Assert.Single(result.Errors);
        Assert.Equal(nameof(QuestionRequest.Message), error.PropertyName);
    }

    [Fact]
    public void Validate_MessageTooLong_IsInvalid()
    {
        var request = new QuestionRequest { Message = new string('a', QuestionRequestValidator.MaxMessageLength + 1) };

        ValidationResult result = _sut.Validate(request);

        Assert.False(result.IsValid);
    }
}
