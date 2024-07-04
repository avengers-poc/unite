using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Jira.Rest.Sdk;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Octokit;
using OpenAI.Assistants;
using Unite.Gpt;

namespace Unite.Controllers;

[ApiController]
[Route("Chat")]
[Experimental("OPENAI001")]
public class ChatController : ControllerBase
{
    private readonly GitHubClient _gitHubClient;
    private readonly JiraService _jiraService;
    private readonly AssistantClient _assistantClient;
    private readonly AssistantService _assistantService;

    public ChatController(GitHubClient gitHubClient, JiraService jiraService, AssistantClient assistantClient, AssistantService assistantService)
    {
        _gitHubClient = gitHubClient;
        _jiraService = jiraService;
        _assistantClient = assistantClient;
        _assistantService = assistantService;
    }

    [HttpPost("SendMessage")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SendMessage(
        [FromQuery] string message,
        [FromQuery] string threadId,
        CancellationToken cancellationToken)
    {
        return Ok(await _assistantService.SendMessage(message, threadId));
    }
}