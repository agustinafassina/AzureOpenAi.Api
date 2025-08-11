using AutoMapper;
using OpenAi.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using OpenAi.Api.Controllers.Contracts.Requests;
using OpenAi.Api.Controllers.Contracts.Response;

namespace OpenAi.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IOpenAIService _openAiService;
        private readonly IMapper _mapper;

        public ChatController(IOpenAIService openAiService, IMapper mapper)
        {
            _openAiService = openAiService;
            _mapper = mapper;
        }

        [HttpPost()]
        public IActionResult GetChatgpt([FromBody] QuestionRequest entity)
        {
            string response = _openAiService.GetQuestion(entity.Message);

            if (string.IsNullOrEmpty(response))
            {
                return NotFound("No response from OpenAI service.");
            }

            ChatResponse newResponse =new ChatResponse
            {
                Reply = response
            };

            return Ok(newResponse);
        }
    }
}