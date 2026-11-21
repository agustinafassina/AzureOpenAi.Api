using System.Security.Claims;
using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using OpenAi.Api;
using OpenAi.Api.Middleware;
using OpenAi.Api.Validators;
using OpenAi.Repository;
using OpenAi.Services;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Dependency Injection: Repositories (then Services)
builder.Services.AddRepositories(configuration);
builder.Services.AddApplicationServices();

// Request validators (FluentValidation)
builder.Services.AddValidatorsFromAssemblyContaining<QuestionRequestValidator>();

// Errors → ProblemDetails (with traceId)
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// CORS: origins come from config. Bearer tokens don't need AllowCredentials.
var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        if (builder.Environment.IsDevelopment() && allowedOrigins.Contains("*"))
        {
            policy.AllowAnyOrigin();
        }
        else
        {
            policy.WithOrigins(allowedOrigins);
        }

        policy.AllowAnyHeader().WithMethods("GET", "POST");
    });
});

// Authentication: one named JWT Bearer scheme per issuer. Settings come from config,
// which also reads environment variables such as Auth0App1__Issuer.
string[] jwtSchemes = ["Auth0App1", "Auth0App2"];
var authentication = builder.Services.AddAuthentication();
foreach (string scheme in jwtSchemes)
{
    authentication.AddJwtBearer(scheme, options =>
    {
        IConfigurationSection section = configuration.GetSection(scheme);
        options.Authority = section["Issuer"];
        options.Audience = section["Audience"];
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = section["Issuer"],
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });
}

// Every endpoint requires a valid token from any configured issuer unless marked [AllowAnonymous].
builder.Services.AddAuthorization(options =>
{
    AuthorizationPolicy authenticatedUser = new AuthorizationPolicyBuilder(jwtSchemes)
        .RequireAuthenticatedUser()
        .Build();
    options.DefaultPolicy = authenticatedUser;
    options.FallbackPolicy = authenticatedUser;
});

// Rate limiting: caps Azure OpenAI spend per user (or per IP if unauthenticated).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(RateLimitPolicies.Chat, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = configuration.GetValue("RateLimiting:Chat:PermitLimit", 20),
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
});

builder.Services.AddControllers();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages(); // empty 401/403/429 responses get a ProblemDetails body
app.UseHttpsRedirection();
app.UseRouting();

// CORS must run after routing and before auth so preflight requests aren't rejected.
app.UseCors("AllowSpecificOrigins");

// Swagger runs before auth so the fallback policy doesn't block the UI.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();
