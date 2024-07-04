using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Jira.Rest.Sdk;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Octokit;
using OpenAI.Assistants;

namespace Unite.Controllers;

[ApiController]
[Route("Chat")]
[Experimental("OPENAI001")]
public class ChatController : ControllerBase
{
    private readonly GitHubClient _gitHubClient;
    private readonly JiraService _jiraService;
    private readonly AssistantClient _assistantClient;

    public ChatController(GitHubClient gitHubClient, JiraService jiraService, AssistantClient assistantClient)
    {
        _gitHubClient = gitHubClient;
        _jiraService = jiraService;
        _assistantClient = assistantClient;
    }

    [HttpPost("SendMessage")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SendMessage(
        [FromQuery] string message,
        CancellationToken cancellationToken)
    {
        return Ok(message);
    }
}