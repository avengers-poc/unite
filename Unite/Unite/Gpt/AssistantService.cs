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

    public async Task<SendMessageResult> SendMessage(string message, string threadId = null)
    {
        var assistant = (await _assistantClient.GetAssistantAsync(AssistantId)).Value;
        ThreadRun run;
        if (!string.IsNullOrWhiteSpace(threadId))
        {
            run = (await _assistantClient.CreateRunAsync(threadId, AssistantId, new RunCreationOptions()
            {
                AdditionalMessages = { message }
            })).Value;
        }
        else
        {
            run = (await _assistantClient.CreateThreadAndRunAsync(assistant, new ThreadCreationOptions()
            {
                InitialMessages = { message }
            })).Value;
        }
        
        do
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            run = await _assistantClient.GetRunAsync(run.ThreadId, run.Id);

            // If the run requires action, resolve them.
            if (run.Status == RunStatus.RequiresAction)
            {
                List<ToolOutput> toolOutputs = [];

                foreach (RequiredAction action in run.RequiredActions)
                {
                    if (action.FunctionName == "SearchGithub")
                    {
                        using JsonDocument argumentsJson = JsonDocument.Parse(action.FunctionArguments);
                        argumentsJson.RootElement.TryGetProperty("searchstring", out JsonElement searchstring);
                        string result = await SearchGithub(searchstring.GetString());
                        toolOutputs.Add(new ToolOutput(action.ToolCallId, result));
                    }

                    if (action.FunctionName == "GetCommitHistory")
                    {
                        using JsonDocument argumentsJson = JsonDocument.Parse(action.FunctionArguments);
                        bool hasFilePath = argumentsJson.RootElement.TryGetProperty("filePath", out JsonElement filePath);
                        string result = hasFilePath
                            ? await GetCommitHistory(filePath.GetString())
                            : await GetCommitHistory();
                        toolOutputs.Add(new ToolOutput(action.ToolCallId, result));
                    }

                    if (action.FunctionName == "GetPullRequests")
                    {
                        string result = await GetPullRequests();
                        toolOutputs.Add(new ToolOutput(action.ToolCallId, result));
                    }
                    
                    if (action.FunctionName == "SearchJira")
                    {
                        using JsonDocument argumentsJson = JsonDocument.Parse(action.FunctionArguments);
                        argumentsJson.RootElement.TryGetProperty("searchstring", out JsonElement searchstring);
                        string result = SearchJira(searchstring.GetString());
                        toolOutputs.Add(new ToolOutput(action.ToolCallId, result));
                    }
                }

                // Submit the tool outputs to the assistant, which returns the run to the queued state.
                run = await _assistantClient.SubmitToolOutputsToRunAsync(run.ThreadId, run.Id, toolOutputs);
            }
        } while (!run.Status.IsTerminal);
        List<ThreadMessage> messages
            = _assistantClient.GetMessages(run.ThreadId, ListOrder.OldestFirst).ToList();
        var response = string.Join("\r\n", messages.Last().Content);
        return new()
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
        IReadOnlyList<PullRequest> pullRequests = await _gitHubClient.Repository.PullRequest.GetAllForRepository(repo.Id);
        return JsonSerializer.Serialize(pullRequests);
    }

    private string SearchJira(string searchString)
    {
        var issues = _jiraService.IssueSearch(searchString);
        return JsonSerializer.Serialize(issues);
    }
}