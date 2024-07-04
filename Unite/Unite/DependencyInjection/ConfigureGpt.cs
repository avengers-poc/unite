using System.ClientModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.Assistants;

namespace Unite.DependencyInjection;

public static class ConfigureGpt
{
    [Experimental("OPENAI001")]
    public static void AddGptAssistant(this IServiceCollection services)
    {
        services.AddScoped<AssistantClient>(_ =>
            new AssistantClient(new ApiKeyCredential("")));

        services.AddScoped<Assistant>(prv =>
        {
            var client = prv.GetRequiredService<AssistantClient>();
            return client.GetAssistant("asst_hJvgr1Z7nr7Qf1cAHwD9ZIdz").Value;
        });
    }
}