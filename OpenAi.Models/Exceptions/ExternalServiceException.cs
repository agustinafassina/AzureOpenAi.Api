namespace OpenAi.Models.Exceptions;

public sealed class ExternalServiceException : Exception
{
    public ExternalServiceException(string message, int? upstreamStatusCode, TimeSpan? retryAfter = null, Exception? innerException = null)
        : base(message, innerException)
    {
        UpstreamStatusCode = upstreamStatusCode;
        RetryAfter = retryAfter;
    }

    public int? UpstreamStatusCode { get; }
    public TimeSpan? RetryAfter { get; }
    public bool IsTransient => UpstreamStatusCode is null or 408 or 429 or >= 500;
}
