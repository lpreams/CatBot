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
        var prefix = builder.Configuration.Prefix();
        builder.Services
            .AddDiscordGateway(options => options.Intents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent)
            .AddCommands<PrefixedCommandContext>(options =>
            {
                options.CreateContext = (message, client, services) =>
                    new PrefixedCommandContext(message, client, prefix);
            });
        var host = builder.Build();
        host.AddCommandModule<HelpBot>();
        if (builder.Configuration.EnableColorBot())
            host.AddCommandModule<ColorBot>();
        if (builder.Configuration.EnableRolesBot())
        {
            var rolesDict = builder.Configuration.RoleGroups();
            if (rolesDict == null || rolesDict.Count == 0)
                throw new Exception("RoleGroups not configured in appsettings.json but EnableRolesBot is true");
            RolesGroupSingleton.Load(builder.Configuration);
            host.AddCommandModule<RolesBot>();
        }

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

public static class Extensions
{
    public static string Prefix(this IConfiguration config) => config["Discord:Prefix"] ?? "?";
    public static bool EnableColorBot(this IConfiguration config) => config.GetValue<bool>("EnableColorBot");
    public static bool EnableRolesBot(this IConfiguration config) => config.GetValue<bool>("EnableRolesBot");

    public static Dictionary<string, ICollection<ulong>> RoleGroups(this IConfiguration config) => (config
            .GetSection("RoleGroups").Get<Dictionary<string, ICollection<ulong>>>() ?? [])
        .Select(e => (e.Key.ToLower(), e.Value))
        .ToDictionary(e => e.Item1, e => e.Item2);

    public static string FirstUpper(this string str) => str.Substring(0, 1).ToUpper() + str.Substring(1).ToLower();
    
    public static List<string> Tableify(this List<List<string>> table)
    {
        var maxLengths = new List<int>();
        foreach (var row in table) {
            for (var col = 0; col < row.Count; ++col) {
                while (maxLengths.Count < col+1)
                    maxLengths.Add(0);
                if (maxLengths[col] < row[col].Length)
                    maxLengths[col] = row[col].Length;
            }   
        }

        return table
            .Select(row =>
                string.Join(" ", Enumerable.Range(0, row.Count)
                        .Select(col =>
                            row[col].PadLeft(maxLengths[col]))
                )).ToList();
    }

}