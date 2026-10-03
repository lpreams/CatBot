using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services.Commands;

namespace CatBot;

class Program
{
    static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        var prefix = builder.Configuration["Discord:Prefix"] ?? "?";
        builder.Services
            .AddDiscordGateway(options => options.Intents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent)
            .AddCommands<PrefixedCommandContext>(options =>
            {
                options.CreateContext = (message, client, services) =>
                    new PrefixedCommandContext(message, client, prefix.Length);
            });
        var host = builder.Build();
        if (builder.Configuration.GetValue<bool>("EnableColorBot"))
            host.AddCommandModule<ColorBot>();
        await host.RunAsync();
    }
}
