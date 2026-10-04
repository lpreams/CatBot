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
            .WithDescription(await HelpImpl(config, Context))));
    }
    [Command("help")]
    public async Task Help(params string[] args)
    {
        await Help();
    }

    public static async Task<string?> HelpImpl(IConfiguration config, PrefixedCommandContext context)
    {
        var messages = new List<string>();
        if (config.EnableColorBot())
        {
            messages.Add($"""
                          ### Colours
                          To set your colour, use a command like `{config.Prefix()}colour #FF00FF`
                          To generate a colour, use a [Colour Picker](https://www.google.com/search?q=color+picker)
                          """);
        }

        
        
        if (config.EnableRolesBot())
        {
            var roles = await context.Guild!.GetRolesAsync();
            var allRolesDict = roles.ToDictionary(r => r.Id, r => r.Name);
            
            foreach (var type in config.RoleGroups().Keys)
            {
                messages.Add($"""
                              ### {type.FirstUpper()}
                              To set your {type.ToLower()}, use a command like `{config.Prefix()}{type.ToLower()} {allRolesDict[config.RoleGroups()[type].First()]}`
                              Available {type.ToLower()} values are: {string.Join(" ", config.RoleGroups()[type].Select(t => $"`{allRolesDict[t]}`"))} 
                              """);
            }
        }

        if (messages.Any())
        {
            return string.Join("\n \n", messages);
        }

        return null;
    }
}