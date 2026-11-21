namespace OpenAi.Api;

public static class RateLimitPolicies
{
    /// <summary>Per-user limit on chat requests, to cap Azure OpenAI spend.</summary>
    public const string Chat = "chat";
}
