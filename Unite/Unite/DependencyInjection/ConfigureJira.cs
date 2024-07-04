using Jira.Rest.Sdk;
using Microsoft.Extensions.DependencyInjection;

namespace Unite.DependencyInjection;

public static class ConfigureJira
{
    public static void AddJira(this IServiceCollection services)
    {
        services.AddScoped<JiraService>(_ => new JiraService(
            "https://avengerspoc.atlassian.net/jira/software/projects/AU/boards/2",
            "avengers.poc@gmail.com",
            "",
            true));
    }
}