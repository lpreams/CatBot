using Microsoft.Extensions.Configuration;
using NetCord.Rest;
using NetCord.Services.Commands;

namespace CatBot;

public class HelpBot(IConfiguration config) : CommandModule<PrefixedCommandContext>
{
    [Command("help")]
    public async Task Help()
    {
        await ReplyAsync(new ReplyMessageProperties().AddEmbeds(new EmbedProperties()
            .WithDescription(HelpImpl(config))));
    }
    [Command("help")]
    public async Task Help(params string[] args)
    {
        await Help();
    }

    public static string? HelpImpl(IConfiguration config)
    {
        var messages = new List<string>();
        if (config.EnableColorBot())
        {
            messages.Add($"""
                          ### Colours
                          To set your colour, use a command like `{config.Prefix()}colour #FF00FF`
                          To generate a colour, use a [colour picker](https://www.google.com/search?q=color+picker)
                          """);
        }

        if (messages.Any())
        {
            return string.Join("\n \n", messages);
        }

        return null;
    }
}