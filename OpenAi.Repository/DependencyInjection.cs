using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAi.Models.Settings;
using OpenAi.Repository.Implementations;
using OpenAi.Repository.Interfaces;
using OpenAI.Chat;

namespace OpenAi.Repository;

public static class DependencyInjection
{
    public static IServiceCollection AddRepositories(this IServiceCollection services, IConfiguration configuration)
    {
        // Fail at startup (not on the first request) if the endpoint, deployment or key is missing.
        services.AddOptions<OpenAISetting>()
            .Bind(configuration.GetSection(OpenAISetting.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // SDK clients are thread-safe and meant to be reused for the app's lifetime.
        services.AddSingleton(sp =>
        {
            OpenAISetting settings = sp.GetRequiredService<IOptions<OpenAISetting>>().Value;
            var azureClient = new AzureOpenAIClient(new Uri(settings.Endpoint), new AzureKeyCredential(settings.ApiKey));
            return azureClient.GetChatClient(settings.DeploymentName);
        });

        services.AddSingleton<IChatCompletionRepository, AzureOpenAIChatRepository>();
        return services;
    }
}
