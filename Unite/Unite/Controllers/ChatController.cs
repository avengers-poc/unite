using System.Diagnostics.CodeAnalysis;

using Microsoft.AspNetCore.Mvc;

using Unite.Gpt;

namespace Unite.Controllers;

[ApiController]
[Route("Chat")]
[Experimental("OPENAI001")]
public class ChatController : ControllerBase
{
    private readonly AssistantService _assistantService;

    public ChatController(AssistantService assistantService)
    {
        _assistantService = assistantService;
    }

    [HttpPost("SendMessage")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SendMessage(
        [FromQuery] string message,
        [FromQuery] string threadId,
        CancellationToken cancellationToken)
    {
        var response = await _assistantService.SendMessage(message, threadId, cancellationToken);
        
        return Ok(response);
    }
}
