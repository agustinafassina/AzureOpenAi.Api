using FluentValidation;
using OpenAi.Api.Contracts.Requests;

namespace OpenAi.Api.Validators;

public class QuestionRequestValidator : AbstractValidator<QuestionRequest>
{
    public const int MaxMessageLength = 4000;

    public QuestionRequestValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message is required.")
            .MaximumLength(MaxMessageLength).WithMessage($"Message must not exceed {MaxMessageLength} characters.");
    }
}
