using Microsoft.Extensions.Configuration;
using NetCord;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.Commands;

namespace CatBot;

public class RolesBot(IConfiguration config) : CommandModule<PrefixedCommandContext>
{
    [Command("listroles")]
    [RequireContext<PrefixedCommandContext>(RequiredContext.Guild)]
    [RequireBotPermissions<PrefixedCommandContext>(Permissions.ManageRoles)]
    [RequireUserPermissions<PrefixedCommandContext>(Permissions.Administrator)]
    public async Task ListRoles()
    {
        var roles = (await Context.Guild!.GetRolesAsync()).Where(r => r.Id != r.GuildId && !r.Managed);

        List<List<string>> table = roles.Select(r => new List<string>{ r.Id.ToString(), r.Name }).ToList();
        
        var str = string.Join("\n", table.Tableify());
        
        await ReplyAsync(new ReplyMessageProperties().AddEmbeds(new EmbedProperties()
            .WithDescription($"""
                             ```
                             {str}
                             ```
                             """)));
    }

    [RoleGroupCommand]
    [RequireContext<PrefixedCommandContext>(RequiredContext.Guild)]
    [RequireBotPermissions<PrefixedCommandContext>(Permissions.ManageRoles)]
    public async Task SetRole(params string[] roleNameArgs)
    {

        var roleName = string.Join(" ", roleNameArgs);
        
        if (roleName.ToLower().Equals("help"))
        {
            await ReplyAsync(new ReplyMessageProperties().AddEmbeds(new EmbedProperties()
                .WithDescription(await HelpBot.HelpImpl(config, Context))));
            return;
        }

        var roleType = Context.InvokedAlias.ToLower();
        if (!RolesGroupSingleton.Groups.TryGetValue(roleType, out var rolesList))
            return;
        
        var roles = new HashSet<ulong>(rolesList);
        
        roleName = roleName.ToLower().Trim();

        var allRoles = (await Context.Guild!.GetRolesAsync()).Where(r => roles.Contains(r.Id));

        var foundRole = allRoles.FirstOrDefault(r => r.Name.ToLower().Trim().Equals(roleName));
        if (foundRole == null)
        {
            await ReplyAsync(new ReplyMessageProperties().AddEmbeds(new EmbedProperties()
                .WithDescription($"{Context.InvokedAlias.FirstUpper()} not found: `{roleName}`")));
            return;
        }
        
        if (!Context.Guild!.Users.TryGetValue(Context.User.Id, out var member))
            member = await Context.Guild!.GetUserAsync(Context.User.Id);

        var hasRole = member.RoleIds.Contains(foundRole.Id);

        if (hasRole)
        {
            // remove the role
            try
            {
                await member.RemoveRoleAsync(foundRole.Id);
                await ReplyAsync(new ReplyMessageProperties().AddEmbeds(new EmbedProperties()
                    .WithDescription($"Removed {Context.InvokedAlias.ToLower()}: <@&{foundRole.Id}>")));
            }
            catch (RestException e)
            {
                await ReplyAsync(new ReplyMessageProperties().AddEmbeds(new EmbedProperties()
                    .WithDescription($"I can't assign <@&{foundRole.Id}>. Make sure my role is above it in the role list.")));
            }
        }
        else
        {
            // add the role
            try
            {
                await member.AddRoleAsync(foundRole.Id);
                await ReplyAsync(new ReplyMessageProperties().AddEmbeds(new EmbedProperties()
                    .WithDescription($"Added {Context.InvokedAlias.ToLower()}: <@&{foundRole.Id}>")));
            }
            catch (RestException e)
            {
                await ReplyAsync(new ReplyMessageProperties().AddEmbeds(new EmbedProperties()
                    .WithDescription($"I can't assign <@&{foundRole.Id}>. Make sure my role is above it in the role list.")));
            }
        }
    }
}

static class RolesGroupSingleton
{
    public static IReadOnlyDictionary<string, ICollection<ulong>> Groups { get; private set; } =
        new Dictionary<string, ICollection<ulong>>();
    public static void Load(IConfiguration config)
    {
        Groups = config.RoleGroups();
    }
}

public class RoleGroupCommandAttribute() : CommandAttribute(RolesGroupSingleton.Groups.Keys.ToArray());