using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OpenAi.Api.Middleware;
using OpenAi.Models.Exceptions;

namespace OpenAi.Api.Tests.Middleware;

public class GlobalExceptionHandlerTests
{
    private static (GlobalExceptionHandler Handler, HttpContext Context) CreateSut()
    {
        ServiceProvider services = new ServiceCollection()
            .AddLogging()
            .AddProblemDetails()
            .BuildServiceProvider();

        var context = new DefaultHttpContext { RequestServices = services };
        context.Response.Body = new MemoryStream();

        var handler = new GlobalExceptionHandler(
            services.GetRequiredService<IProblemDetailsService>(),
            NullLogger<GlobalExceptionHandler>.Instance);

        return (handler, context);
    }

    private static async Task<JsonElement> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return await JsonSerializer.DeserializeAsync<JsonElement>(context.Response.Body);
    }

    [Theory]
    [InlineData(429)]
    [InlineData(500)]
    [InlineData(503)]
    public async Task TryHandleAsync_TransientExternalFailure_Returns503(int upstreamStatus)
    {
        (GlobalExceptionHandler handler, HttpContext context) = CreateSut();

        bool handled = await handler.TryHandleAsync(context, new ExternalServiceException("x", upstreamStatus), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_NoUpstreamResponse_Returns503()
    {
        (GlobalExceptionHandler handler, HttpContext context) = CreateSut();

        await handler.TryHandleAsync(context, new ExternalServiceException("x", upstreamStatusCode: null), CancellationToken.None);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
    }

    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    public async Task TryHandleAsync_NonTransientExternalFailure_Returns502(int upstreamStatus)
    {
        (GlobalExceptionHandler handler, HttpContext context) = CreateSut();

        await handler.TryHandleAsync(context, new ExternalServiceException("x", upstreamStatus), CancellationToken.None);

        Assert.Equal(StatusCodes.Status502BadGateway, context.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_ExternalFailureWithRetryAfter_SetsRetryAfterHeader()
    {
        (GlobalExceptionHandler handler, HttpContext context) = CreateSut();

        await handler.TryHandleAsync(context, new ExternalServiceException("x", 429, TimeSpan.FromSeconds(12)), CancellationToken.None);

        Assert.Equal("12", context.Response.Headers.RetryAfter.ToString());
    }

    [Fact]
    public async Task TryHandleAsync_UnexpectedException_Returns500WithoutLeakingMessage()
    {
        (GlobalExceptionHandler handler, HttpContext context) = CreateSut();

        await handler.TryHandleAsync(context, new InvalidOperationException("secret connection string"), CancellationToken.None);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        JsonElement body = await ReadBodyAsync(context);
        Assert.Equal(500, body.GetProperty("status").GetInt32());
        Assert.True(body.TryGetProperty("traceId", out _));
        Assert.DoesNotContain("secret", body.GetRawText());
    }

    [Fact]
    public async Task TryHandleAsync_ClientAborted_Returns499WithoutBody()
    {
        (GlobalExceptionHandler handler, HttpContext context) = CreateSut();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        context.RequestAborted = cts.Token;

        bool handled = await handler.TryHandleAsync(context, new OperationCanceledException(), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status499ClientClosedRequest, context.Response.StatusCode);
        Assert.Equal(0, context.Response.Body.Length);
    }
}
