using Microsoft.Extensions.DependencyInjection;
using OpenAi.Services.Implementations;
using OpenAi.Services.Interfaces;

namespace OpenAi.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<IOpenAIService, OpenAIService>();
        return services;
    }
}
