using EMSApp.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace EMSApp.Api.Controllers
{
    [ApiController]
    [Route("api/ai")]
    public class AIController : ControllerBase
    {
        private readonly IChatBotService _openAIService;

        public AIController(IChatBotService openAIService)
        {
            _openAIService = openAIService;
        }

        [HttpPost("test")]
        public async Task<IActionResult> TestPrompt([FromBody] AIRequestDto dto, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.Prompt))
                return BadRequest("The prompt cannot be empty");

            var responseText = await _openAIService.GetChatResponseAsync(dto.Prompt, ct);
            return Ok(new { result = responseText });
        }
    }
    public class AIRequestDto
    {
        public string Prompt { get; set; } = string.Empty;
    }
}
