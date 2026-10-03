using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services.Commands;
using NetCord.Services.Commands;

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
                    new PrefixedCommandContext(message, client, prefix);
            });
        var host = builder.Build();
        if (builder.Configuration.GetValue<bool>("EnableColorBot"))
            host.AddCommandModule<ColorBot>();
        await host.RunAsync();
    }
}

/// <summary>
/// Allows to get the alias that was used to invoke a Command
/// </summary>
/// <param name="message"></param>
/// <param name="client"></param>
/// <param name="prefixLength"></param>
public class PrefixedCommandContext(Message message, GatewayClient client, string prefix)
    : CommandContext(message, client)
{
    // The alias as the user typed it, e.g. "colour" for "?colour #fff"
    public string InvokedAlias => Message.Content[prefix.Length..]
        .TrimStart()
        .Split(' ', 2)[0];
}