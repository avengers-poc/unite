using Microsoft.Extensions.DependencyInjection;
using Octokit;

namespace Unite.DependencyInjection;

public static class ConfigureGithub
{
    public static void AddGithub(this IServiceCollection services)
    {
        services.AddScoped<GitHubClient>(_ =>
        {
            var token = new Credentials("");
            var client = new GitHubClient(new ProductHeaderValue("Unite"));
            client.Credentials = token;
            return client;
        });
    }
}