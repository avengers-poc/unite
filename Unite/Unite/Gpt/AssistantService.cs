using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using Jira.Rest.Sdk;

using Octokit;

using OpenAI;
using OpenAI.Assistants;

namespace Unite.Gpt;

[Experimental("OPENAI001")]
public class AssistantService
{
    private readonly AssistantClient _assistantClient;
    private readonly GitHubClient _gitHubClient;
    private readonly JiraService _jiraService;

    private const string AssistantId = "asst_hJvgr1Z7nr7Qf1cAHwD9ZIdz";
    private const string Owner = "avengers-poc";
    private const string Repo = "unite";

    public AssistantService(AssistantClient assistantClient, GitHubClient gitHubClient, JiraService jiraService)
    {
        _assistantClient = assistantClient;
        _gitHubClient = gitHubClient;
        _jiraService = jiraService;
    }

    public async Task<SendMessageResult> SendMessage(string message, string threadId = null, CancellationToken cancellationToken = default)
    {
        var assistant = (await _assistantClient.GetAssistantAsync(AssistantId)).Value;
        
        ThreadRun run;
        
        if (!string.IsNullOrWhiteSpace(threadId))
        {
            run = (await _assistantClient.CreateRunAsync(threadId, AssistantId, new RunCreationOptions
            {
                AdditionalMessages = { message }
            },
            cancellationToken)).Value;
        }
        else
        {
            run = (await _assistantClient.CreateThreadAndRunAsync(assistant, new ThreadCreationOptions
            {
                InitialMessages = { message }
            })).Value;
        }
        
        do
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            
            run = await _assistantClient.GetRunAsync(run.ThreadId, run.Id, cancellationToken);

            // If the run requires action, resolve them.
            if (run.Status != RunStatus.RequiresAction) continue;
            
            List<ToolOutput> toolOutputs = [];

            foreach (var action in run.RequiredActions)
            {
                if (action.FunctionName == "SearchGithub")
                {
                    using var argumentsJson = JsonDocument.Parse(action.FunctionArguments);
                    argumentsJson.RootElement.TryGetProperty("searchstring", out var searchstring);
                    var result = await SearchGithub(searchstring.GetString());
                    toolOutputs.Add(new ToolOutput(action.ToolCallId, result));
                }

                if (action.FunctionName == "GetCommitHistory")
                {
                    using var argumentsJson = JsonDocument.Parse(action.FunctionArguments);
                    var hasFilePath = argumentsJson.RootElement.TryGetProperty("filePath", out var filePath);
                    var result = hasFilePath
                        ? await GetCommitHistory(filePath.GetString())
                        : await GetCommitHistory();
                    toolOutputs.Add(new ToolOutput(action.ToolCallId, result));
                }

                if (action.FunctionName == "GetPullRequests")
                {
                    var result = await GetPullRequests();
                    toolOutputs.Add(new ToolOutput(action.ToolCallId, result));
                }
                
                if (action.FunctionName == "SearchJira")
                {
                    using var argumentsJson = JsonDocument.Parse(action.FunctionArguments);
                    argumentsJson.RootElement.TryGetProperty("searchstring", out var searchstring);
                    var result = SearchJira(searchstring.GetString());
                    toolOutputs.Add(new ToolOutput(action.ToolCallId, result));
                }
                
                if (action.FunctionName == "GetSolutionArchitecture")
                {
                    var result = "Solution Architecture";
                    toolOutputs.Add(new ToolOutput(action.ToolCallId, JsonSerializer.Serialize(true)));
                }
                
                if (action.FunctionName == "CreateJiraIssue")
                {
                    using var argumentsJson = JsonDocument.Parse(action.FunctionArguments);
                    argumentsJson.RootElement.TryGetProperty("issueType", out var issueType);
                    argumentsJson.RootElement.TryGetProperty("summary", out var summary);
                    argumentsJson.RootElement.TryGetProperty("issueDescription", out var description);
                    argumentsJson.RootElement.TryGetProperty("priority", out var priority);
                    argumentsJson.RootElement.TryGetProperty("parentKey", out var parentKey);
                    var result = CreateJiraIssue(issueType.GetString(), summary.GetString(), description.GetString(), priority.GetString(), string.IsNullOrWhiteSpace(parentKey.GetString()) ? null : parentKey.GetString());
                    toolOutputs.Add(new ToolOutput(action.ToolCallId, result));
                }
            }

            // Submit the tool outputs to the assistant, which returns the run to the queued state.
            run = await _assistantClient.SubmitToolOutputsToRunAsync(
                run.ThreadId,
                run.Id,
                toolOutputs,
                cancellationToken);
        }
        while (!run.Status.IsTerminal);
        
        var messages = _assistantClient
            .GetMessages(run.ThreadId, ListOrder.OldestFirst, cancellationToken)
            .ToList();
        
        var response = string.Join("\r\n", messages.Last().Content);
        
        return new SendMessageResult
        {
            Response = response,
            ThreadId = run.ThreadId
        };
    }

    private async Task<string> SearchGithub(string searchstring)
    {
        var resp = await _gitHubClient.Search.SearchCode(new(searchstring, Owner, Repo));
        return JsonSerializer.Serialize(resp);
    }

    private async Task<string> GetCommitHistory(string fileName = null)
    {
        var repo = await _gitHubClient.Repository.Get(Owner, Repo);
        var commits = await _gitHubClient.Repository.Commit.GetAll(repo.Id, new CommitRequest()
        {
            Path = fileName
        });
        return JsonSerializer.Serialize(commits);
    }

    private async Task<string> GetPullRequests()
    {
        var repo = await _gitHubClient.Repository.Get(Owner, Repo);
        var pullRequests = await _gitHubClient.Repository.PullRequest.GetAllForRepository(repo.Id);
        return JsonSerializer.Serialize(pullRequests);
    }

    private string SearchJira(string searchString)
    {
        var issues = _jiraService.IssueSearch(searchString);
        return JsonSerializer.Serialize(issues);
    }

    private string CreateJiraIssue(
        string issueType,
        string summary,
        string description = null,
        string priority = null,
        string parentKey = null
        )
    {
        var issue = _jiraService.IssueCreate("AU", issueType, summary, description, priority, parentKey);
        return JsonSerializer.Serialize(issue);
    }    
}