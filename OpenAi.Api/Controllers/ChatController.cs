using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenAi.Api.Contracts.Requests;
using OpenAi.Api.Contracts.Responses;
using OpenAi.Services.Interfaces;

namespace OpenAi.Api.Controllers;

// Authentication is required by the fallback authorization policy (see Program.cs).
[ApiController]
[Route("api/v1/[controller]")]
[EnableRateLimiting(RateLimitPolicies.Chat)]
public class ChatController : ControllerBase
{
    private readonly IOpenAIService _openAiService;
    private readonly IValidator<QuestionRequest> _validator;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IOpenAIService openAiService, IValidator<QuestionRequest> validator, ILogger<ChatController> logger)
    {
        _openAiService = openAiService;
        _validator = validator;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType<ChatResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> AskAsync([FromBody] QuestionRequest request, CancellationToken cancellationToken)
    {
        FluentValidation.Results.ValidationResult validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            Dictionary<string, string[]> errors = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return ValidationProblem(new ValidationProblemDetails(errors));
        }

        string? reply = await _openAiService.AskAsync(request.Message, cancellationToken);

        if (reply is null)
        {
            _logger.LogWarning("Returning 502: the chat model produced no reply");
            return Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "The AI service returned an empty response.");
        }

        return Ok(new ChatResponse { Reply = reply });
    }
}
