using System.Globalization;
using Microsoft.AspNetCore.Diagnostics;
using OpenAi.Models.Exceptions;

namespace OpenAi.Api.Middleware;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            // The client went away; nobody is left to read a response.
            _logger.LogInformation("Request was cancelled by the client");
            httpContext.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
            return true;
        }

        (int status, string title) = exception switch
        {
            ExternalServiceException { IsTransient: true } =>
                (StatusCodes.Status503ServiceUnavailable, "The AI service is temporarily unavailable. Please try again later."),
            ExternalServiceException =>
                (StatusCodes.Status502BadGateway, "The AI service returned an error."),
            _ =>
                (StatusCodes.Status500InternalServerError, "An unexpected error occurred."),
        };

        if (exception is ExternalServiceException external)
        {
            _logger.LogWarning(exception, "External service failure. UpstreamStatus={UpstreamStatus}", external.UpstreamStatusCode);

            if (external.RetryAfter is { } retryAfter)
            {
                httpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
            }
        }
        else
        {
            _logger.LogError(exception, "Unhandled exception");
        }

        httpContext.Response.StatusCode = status;
        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = { Status = status, Title = title },
        });
    }
}
