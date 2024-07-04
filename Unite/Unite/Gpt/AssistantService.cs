using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jira.Rest.Sdk;
using Octokit;
using OpenAI;
using OpenAI.Assistants;
using OpenAI.Chat;

namespace Unite.Gpt;

[Experimental("OPENAI001")]
public class AssistantService
{
    private readonly AssistantClient _assistantClient;
    private readonly GitHubClient _gitHubClient;
    private readonly JiraService _jiraService;

    private const string AssistantId = "asst_hJvgr1Z7nr7Qf1cAHwD9ZIdz";

    public AssistantService(AssistantClient assistantClient, GitHubClient gitHubClient, JiraService jiraService)
    {
        _assistantClient = assistantClient;
        _gitHubClient = gitHubClient;
        _jiraService = jiraService;
    }

    public async Task<string> SendMessage(string message)
    {
        var assistant = (await _assistantClient.GetAssistantAsync(AssistantId)).Value;
        var run = (await _assistantClient.CreateThreadAndRunAsync(assistant, new ThreadCreationOptions()
        {
            InitialMessages = { message }
        })).Value;
        do
        {
            Thread.Sleep(TimeSpan.FromSeconds(1));
            run = _assistantClient.GetRun(run.ThreadId, run.Id);

            // If the run requires action, resolve them.
            if (run.Status == RunStatus.RequiresAction)
            {
                List<ToolOutput> toolOutputs = [];

                foreach (RequiredAction action in run.RequiredActions)
                {
                    if (action.FunctionName == "SearchGithub")
                    {
                        using JsonDocument argumentsJson = JsonDocument.Parse(action.FunctionArguments);
                        bool hasLocation = argumentsJson.RootElement.TryGetProperty("searchstring", out JsonElement searchstring);
                        string result = await SearchGithub(searchstring.GetString());
                        toolOutputs.Add(new ToolOutput(action.ToolCallId, result));
                    }
                }

                // Submit the tool outputs to the assistant, which returns the run to the queued state.
                run = _assistantClient.SubmitToolOutputsToRun(run.ThreadId, run.Id, toolOutputs);
            }
        } while (!run.Status.IsTerminal);
        List<ThreadMessage> messages
            = _assistantClient.GetMessages(run.ThreadId, ListOrder.OldestFirst).ToList();
        return string.Join("\r\n", messages.Last().Content);
    }

    private async Task<string> SearchGithub(string message)
    {
        var resp = await _gitHubClient.Search.SearchCode(new(message, "avengers-poc", "unite"));
        return JsonSerializer.Serialize(resp);
    }
}