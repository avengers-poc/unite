using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Aspose.Cells;
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
    private const string TranslaterAssistantId = "asst_XcM7hTdKewNWPcyVECrgctox";
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
                string result = "";
                
                try
                {
                    if (action.FunctionName == "SearchGithub")
                    {
                        using var argumentsJson = JsonDocument.Parse(action.FunctionArguments);
                        argumentsJson.RootElement.TryGetProperty("searchstring", out var searchstring);
                        result = await SearchGithub(searchstring.GetString());
                    }

                    if (action.FunctionName == "GetCommitHistory")
                    {
                        using var argumentsJson = JsonDocument.Parse(action.FunctionArguments);
                        var hasFilePath = argumentsJson.RootElement.TryGetProperty("filePath", out var filePath);
                        result = hasFilePath
                            ? await GetCommitHistory(filePath.GetString())
                            : await GetCommitHistory();
                    }

                    if (action.FunctionName == "GetPullRequests")
                    {
                        result = await GetPullRequests();
                    }

                    if (action.FunctionName == "SearchJira")
                    {
                        using var argumentsJson = JsonDocument.Parse(action.FunctionArguments);
                        argumentsJson.RootElement.TryGetProperty("searchstring", out var searchstring);
                        result = SearchJira(searchstring.GetString());
                    }

                    if (action.FunctionName == "GetSolutionArchitecture")
                    {
                        result = "Solution Architecture";
                    }

                    if (action.FunctionName == "CreateJiraIssue")
                    {
                        using var argumentsJson = JsonDocument.Parse(action.FunctionArguments);
                        argumentsJson.RootElement.TryGetProperty("issueType", out var issueType);
                        argumentsJson.RootElement.TryGetProperty("summary", out var summary);
                        argumentsJson.RootElement.TryGetProperty("issueDescription", out var description);
                        argumentsJson.RootElement.TryGetProperty("priority", out var priority);

                        string parentKey = null;
                    
                        if (argumentsJson.RootElement.TryGetProperty("parentKey", out var parentKeyJson))
                        {
                            parentKey = parentKeyJson.GetString();

                            if (string.IsNullOrWhiteSpace(parentKey))
                                parentKey = null;
                        }
                    
                        result = CreateJiraIssue(
                            issueType.GetString(),
                            summary.GetString(),
                            description.GetString(),
                            priority.GetString(),
                            parentKey);
                    }

                    if (action.FunctionName == "GetPullRequestCommits")
                    {
                        using var argumentsJson = JsonDocument.Parse(action.FunctionArguments);
                        argumentsJson.RootElement.TryGetProperty("prNumber", out var prNumber);
                        result = await GetPullRequestCommits(Convert.ToInt32(prNumber.ToString()));
                    }

                    if (action.FunctionName == "GetPullRequestFiles")
                    {
                        using var argumentsJson = JsonDocument.Parse(action.FunctionArguments);
                        argumentsJson.RootElement.TryGetProperty("prNumber", out var prNumber);
                        result = await GetPullRequestFiles(Convert.ToInt32(prNumber.ToString()));
                    }

                    if (action.FunctionName == "GetPullRequestComments")
                    {
                        using var argumentsJson = JsonDocument.Parse(action.FunctionArguments);
                        argumentsJson.RootElement.TryGetProperty("prNumber", out var prNumber);
                        result = await GetPullRequestComments(Convert.ToInt32(prNumber.ToString()));
                    }

                    if (action.FunctionName == "GetPullRequestReviews")
                    {
                        using var argumentsJson = JsonDocument.Parse(action.FunctionArguments);
                        argumentsJson.RootElement.TryGetProperty("prNumber", out var prNumber);
                        result = await GetPullRequestReviews(Convert.ToInt32(prNumber.ToString()));
                    }

                    if (action.FunctionName == "CommentOnPullRequest")
                    {
                        using var argumentsJson = JsonDocument.Parse(action.FunctionArguments);
                        argumentsJson.RootElement.TryGetProperty("prNumber", out var prNumber);
                        argumentsJson.RootElement.TryGetProperty("body", out var body);
                        argumentsJson.RootElement.TryGetProperty("commitId", out var commitId);
                        argumentsJson.RootElement.TryGetProperty("path", out var path);
                        argumentsJson.RootElement.TryGetProperty("position", out var position);
                        result = await CommentOnPullRequest(
                            Convert.ToInt32(prNumber.ToString()),
                            body.ToString(),
                            commitId.ToString(),
                            path.ToString(),
                            Convert.ToInt32(position.ToString()));
                    }

                    if (action.FunctionName == "ReplyToCommentOnPullRequest")
                    {
                        using var argumentsJson = JsonDocument.Parse(action.FunctionArguments);
                        argumentsJson.RootElement.TryGetProperty("prNumber", out var prNumber);
                        argumentsJson.RootElement.TryGetProperty("body", out var body);
                        argumentsJson.RootElement.TryGetProperty("inReplyTo", out var inReplyTo);
                        
                        var replyComment = await TranslateAndRefineUserInputAsync(body.ToString(), cancellationToken);
                        
                        result = await ReplyToCommentOnPullRequest(
                            Convert.ToInt32(prNumber.ToString()),
                            replyComment,
                            Convert.ToInt64(inReplyTo.ToString()));
                    }

                    if (action.FunctionName == "ReviewPullRequest")
                    {
                        using var argumentsJson = JsonDocument.Parse(action.FunctionArguments);
                        argumentsJson.RootElement.TryGetProperty("prNumber", out var prNumber);
                        argumentsJson.RootElement.TryGetProperty("body", out var body);
                        argumentsJson.RootElement.TryGetProperty("commitId", out var commitId);
                        var hasReviewComments = argumentsJson.RootElement.TryGetProperty("reviewComments", out var reviewCommentsJson);
                        argumentsJson.RootElement.TryGetProperty("reviewEvent", out var reviewEventString);
                        List<ReviewComment> reviewComments = hasReviewComments
                            ? JsonSerializer.Deserialize<List<ReviewComment>>(reviewCommentsJson)
                            : new List<ReviewComment>();
                        PullRequestReviewEvent reviewEvent = Enum.Parse<PullRequestReviewEvent>(reviewEventString.ToString());
                        
                        result = await ReviewPullRequest(
                            Convert.ToInt32(prNumber.ToString()),
                            body.ToString(),
                            commitId.ToString(),
                            reviewComments.Select(x => new DraftPullRequestReviewComment(x.Body, x.path, x.Position)).ToList(),
                            reviewEvent);
                    }

                    if (!string.IsNullOrWhiteSpace(result))
                    {
                        toolOutputs.Add(new ToolOutput(action.ToolCallId, result));
                    }
                }
                catch (Exception e)
                {
                    ErrorResult errorResult = new()
                    {
                        Message = e.Message,
                        StackTrace = e.StackTrace
                    };
                    string errorResultString = JsonSerializer.Serialize(errorResult);
                    toolOutputs.Add(new ToolOutput(action.ToolCallId, errorResultString));
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

        var responseMessage = messages.LastOrDefault();
        
        var response =
            responseMessage.Role == MessageRole.Assistant
                ? string.Join("\r\n", messages.Last().Content)
                : "No response or an error occurred";
        
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
        var commits = await _gitHubClient.Repository.Commit.GetAll(repo.Id, new CommitRequest
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

    private async Task<string> GetPullRequestCommits(int prNumber)
    {
        var commits = await _gitHubClient.Repository.PullRequest.Commits(Owner, Repo, prNumber);
        return JsonSerializer.Serialize(commits);
    }
    
    private async Task<string> GetPullRequestFiles(int prNumber)
    {
        IReadOnlyList<PullRequestFile> files = await _gitHubClient.Repository.PullRequest.Files(Owner, Repo, prNumber);
        return JsonSerializer.Serialize(files);
    }

    private async Task<string> GetPullRequestComments(int prNumber)
    {
        var comments = await _gitHubClient.Repository.PullRequest.ReviewComment.GetAll(Owner, Repo, prNumber);
        return JsonSerializer.Serialize(comments);
    }

    private async Task<string> GetPullRequestReviews(int prNumber)
    {
        IReadOnlyList<PullRequestReview> comments = await _gitHubClient.Repository.PullRequest.Review.GetAll(Owner, Repo, prNumber);
        return JsonSerializer.Serialize(comments);
    }
    
    private async Task<string> CommentOnPullRequest(int prNumber, string body, string commitId, string path, int position)
    {
        var pullRequestComment = await _gitHubClient.Repository.PullRequest.ReviewComment.Create(Owner, Repo, prNumber,
            new PullRequestReviewCommentCreate(
                body, commitId, path, position));
        return JsonSerializer.Serialize(pullRequestComment);
    }
    
    private async Task<string> ReplyToCommentOnPullRequest(int prNumber, string body, long inReplyTo)
    {
        var pullRequestComment = await _gitHubClient.Repository.PullRequest.ReviewComment.CreateReply(Owner, Repo, prNumber,
            new PullRequestReviewCommentReplyCreate(
                body, inReplyTo));
        return JsonSerializer.Serialize(pullRequestComment);
    }

    private async Task<string> ReviewPullRequest(
        int prNumber,
        string body,
        string commitId,
        List<DraftPullRequestReviewComment> reviewComments,
        PullRequestReviewEvent reviewEvent)
    {
        if (string.IsNullOrWhiteSpace(commitId))
        {
            IReadOnlyList<PullRequestCommit> commits = await _gitHubClient.Repository.PullRequest.Commits(Owner, Repo, prNumber);
            commitId = commits.MaxBy(commit => commit.Commit.Committer.Date).Sha;
        }
        var pullRequestReview = await _gitHubClient.Repository.PullRequest.Review.Create(Owner, Repo, prNumber,
            new PullRequestReviewCreate()
            {
                Body = body,
                CommitId = commitId,
                Comments = reviewComments,
                Event = reviewEvent
            });
        return JsonSerializer.Serialize(pullRequestReview);
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

    private async Task<string> TranslateAndRefineUserInputAsync(string message, CancellationToken cancellationToken = default)
    {
        var translationRun = (await _assistantClient.CreateThreadAndRunAsync(TranslaterAssistantId,
            new ThreadCreationOptions
            {
                InitialMessages = { message }
            },
            cancellationToken: cancellationToken)).Value;

        while (!translationRun.Status.IsTerminal)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            translationRun = await _assistantClient.GetRunAsync(translationRun.ThreadId, translationRun.Id, cancellationToken);
        }
                        
        var translationMessages = _assistantClient
            .GetMessages(translationRun.ThreadId, ListOrder.OldestFirst, cancellationToken)
            .ToList();

        return string.Join("\r\n", translationMessages.Last().Content);
    }
}
