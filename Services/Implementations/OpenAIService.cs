
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Options;
using OpenAi.Api.Services.Dto;
using OpenAi.Api.Services.Entities;
using OpenAi.Api.Services.Interfaces;
using OpenAI.Chat;

namespace OpenAi.Api.Services.Implementations
{
    public class OpenAIService : IOpenAIService
    {
        private readonly OpenAISetting _openAiSetting;

        public OpenAIService(IOptions<OpenAISetting> openAiSetting)
        {
            _openAiSetting = openAiSetting.Value ?? throw new ArgumentNullException(nameof(openAiSetting), "OpenAI settings cannot be null.");
        }

        public string GetQuestion(string question)
        {
            var endpoint = new Uri(_openAiSetting.Endpoint);
            var deploymentName = _openAiSetting.DeploymentName;
            var apiKey = _openAiSetting.ApiKey;

            if (string.IsNullOrEmpty(endpoint.ToString()) || string.IsNullOrEmpty(deploymentName) || string.IsNullOrEmpty(apiKey))
            {
                throw new ArgumentException("OpenAI settings are not properly configured.");
            }

            AzureOpenAIClient azureClient = new( endpoint, new AzureKeyCredential(apiKey) );
            ChatClient chatClient = azureClient.GetChatClient(deploymentName);

            var requestOptions = new ChatCompletionOptions()
            {
                Temperature = 1.0f,
                TopP = 1.0f,
                FrequencyPenalty = 0.0f,
                PresencePenalty = 0.0f,
            };

            List<ChatMessage> messages = new List<ChatMessage>()
            {
                //new SystemChatMessage("Eres una asistente muy útil"),
                new UserChatMessage(question),
            };

            var response = chatClient.CompleteChat(messages, requestOptions);
            System.Console.WriteLine(response.Value.Content[0].Text);

            return response.Value.Content[0].Text;
        }
    }
}