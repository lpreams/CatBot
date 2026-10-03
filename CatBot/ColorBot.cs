using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using NetCord;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.Commands;

namespace CatBot;

public class ColorBot(IConfiguration config) : CommandModule<PrefixedCommandContext>
{
    [Command("color", "colour")]
    [RequireContext<PrefixedCommandContext>(RequiredContext.Guild)]
    [RequireBotPermissions<PrefixedCommandContext>(Permissions.ManageRoles)]
    public async Task Color(string hexColor)
    {
        if (hexColor.ToLower().Equals("help"))
        {
            await ReplyAsync(new ReplyMessageProperties().AddEmbeds(new EmbedProperties()
                .WithDescription(HelpBot.HelpImpl(config))));
            return;
        }
        
        // make sure color is valid
        var hex = ValidateHexColor(hexColor);
        if (hex == null)
        {
            await ReplyAsync(new ReplyMessageProperties().AddEmbeds(new EmbedProperties()
                .WithDescription($"Invalid hex {Context.InvokedAlias.ToLower()}: `{hexColor}`" + Help(Context.InvokedAlias.ToLower()))));
            return;
        }

        //get the user who sent the message
        var guildUser = await Context.Guild!.GetUserAsync(Context.User.Id);
        //get that user's existing color role(s)
        var existingColorRoles = guildUser.GetRoles(Context.Guild!).Where(e => IsValidColorRoleName(e.Name));
        
        var roleColor = CatBotColor.FromHex(hex);
        var roleName = roleColor.RoleName;

        Role? userAlreadyInRole = null;

        var removedRoles = new List<Role>();
        
        foreach (var r in existingColorRoles)
        {
            if (r.Name == roleName)
            {
                userAlreadyInRole = r;
                continue;
            }

            await guildUser.RemoveRoleAsync(r.Id);
            removedRoles.Add(r);
        }

        // if this user was the last user using a removed role, then delete the role from the server
        if (removedRoles.Any())
        {
            var roleCounts = await GetRoleMemberCountsAsync(Context.Guild.Id, Context.Client.Rest.Token!.RawToken);
            foreach (var emptyRole in removedRoles.Where(r => roleCounts.ContainsKey(r.Id) && roleCounts[r.Id] == 0))
            {
                await Context.Guild!.DeleteRoleAsync(emptyRole.Id);
            }
        }
        
        if (userAlreadyInRole == null)
        {
            // add the user to the role
            var newRole = await GetOrCreateRole(roleColor);
            await guildUser.AddRoleAsync(newRole.Id);
            await ReplyAsync(new ReplyMessageProperties().AddEmbeds(new EmbedProperties()
                .WithDescription($"Your {Context.InvokedAlias.ToLower()} has been updated to <@&{newRole.Id}>" + Help(Context.InvokedAlias.ToLower()))
                .WithColor(roleColor.Color)));

            return;
        }

        await ReplyAsync(new ReplyMessageProperties().AddEmbeds(new EmbedProperties()
            .WithDescription($"Your {Context.InvokedAlias.ToLower()} is already set to <@&{userAlreadyInRole.Id}>" + Help(Context.InvokedAlias.ToLower()))
            .WithColor(roleColor.Color)));
    }
    
    private string Help(string alias) =>
        $"\nNeed help? Examples: `#ff00ff`, `123456`, `0xf0f0f0`, etc. [{alias} picker](https://www.google.com/search?q=color+picker)";

    /// <summary>
    /// Gets an existing role for a given color, or creates it if needed
    /// </summary>
    /// <param name="roleColor"></param>
    /// <returns></returns>
    private async Task<Role> GetOrCreateRole(CatBotColor roleColor)
    {
        var roleName = roleColor.RoleName;
        var existingRoles = await Context.Guild!.GetRolesAsync();

        var existingRole = existingRoles.FirstOrDefault(role => role.Name == roleName);
        if (existingRole == null)
        {
            // will need to create new role
            return await Context.Guild!.CreateRoleAsync(new RoleProperties
            {
                Name = roleName,
                Colors = roleColor.RoleColorsProperties,
                Hoist = false, // don't show separately in member list
                Mentionable = false, // no reason to allow color-based mentions
            });
        }
        else
        {
            // check that role has correct color
            if (existingRole.Colors.PrimaryColor.Red != roleColor.R ||
                existingRole.Colors.PrimaryColor.Green != roleColor.G ||
                existingRole.Colors.PrimaryColor.Blue != roleColor.B)
            {
                await existingRole.ModifyAsync(options => options.Colors = roleColor.RoleColorsProperties);
            }

            return existingRole;
        }
    }
    
    /// <summary>
    /// Check if a given string is a valid Color Role name, eg #FF00FF, must start with # and be all uppercase
    /// </summary>
    /// <param name="roleName"></param>
    /// <returns></returns>
    private bool IsValidColorRoleName(string roleName)
    {
        if (roleName.Length != 7)
            return false;
        if (!roleName.StartsWith('#'))
            return false;
        for (var i=1; i<7; ++i)
            if (!char.IsAsciiHexDigitUpper(roleName[i]))
                return false;

        return true;
    }
    
    /// <summary>
    /// Validates and cleans up a hex color string, to only contain 6 uppercase hex digits
    /// </summary>
    /// <param name="hexColor"></param>
    /// <returns></returns>
    private string? ValidateHexColor(string hexColor)
    {
        var hex = hexColor;

        if (hex.StartsWith('`') && hex.EndsWith('`'))
            hex = hex[1..^1];

        if (hex.StartsWith('#'))
            hex = hex[1..];
        else if (hex.StartsWith("0x"))
            hex = hex[2..];

        if (hex.Length != 6 || !hex.All(char.IsAsciiHexDigit))
            return null;

        return hex.ToUpper();
    }
    
    /// <summary>
    /// Represents a 3-byte RGB color
    /// </summary>
    private class CatBotColor
    {
        public byte R { get; init; }
        public byte G { get; init; }
        public byte B { get; init; }

        public string RoleName => $"#{R:X2}{G:X2}{B:X2}";

        public Color Color => new Color(R, G, B);
        public RoleColorsProperties RoleColorsProperties => new RoleColorsProperties(Color);
        public static CatBotColor FromHex(string validHexColor) => new CatBotColor
        {
            R = Convert.ToByte(validHexColor.Substring(0, 2), 16),
            G = Convert.ToByte(validHexColor.Substring(2, 2), 16),
            B = Convert.ToByte(validHexColor.Substring(4, 2), 16)
        };
    }

    /// <summary>
    /// Apparently this is a new Discord API endpoint, NetCord doesn't support it yet, so we're calling it manually
    /// </summary>
    /// <param name="guildId"></param>
    /// <param name="botToken"></param>
    /// <returns></returns>
    private static async Task<Dictionary<ulong, int>> GetRoleMemberCountsAsync(ulong guildId, string botToken)
    {
        using var http = new HttpClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://discord.com/api/v10/guilds/{guildId}/roles/member-counts");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bot", botToken);

        using var response = await http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Dictionary<ulong, int>>()
               ?? new Dictionary<ulong, int>();
    }
}