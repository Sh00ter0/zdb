using Application.Services.Discord;
using Discord;
using Discord.WebSocket;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Extensions;
using Infrastructure.Views.Components;
using Infrastructure.Views.Layouts;

namespace Infrastructure.Discord.SlashCommands.Commands.System.Administration;

public sealed class AdministrationUiBuilder(DiscordSocketClient client, IDiscordEmoteService emoteCache)
{
    public MessageComponent CreateOverviewContainer(SystemAdministrators adminEntity, IUser discordUser,
        Action<ContainerBuilder>? appendComponents = null)
    {
        var activeIcon = emoteCache.GetEmote("UI_ICON_BULB_ON");
        var inactiveIcon = emoteCache.GetEmote("UI_ICON_BULB_OFF");
        var discordCreatedAtTimestamp = $"<t:{((DateTimeOffset)adminEntity.CreatedAtUtc).ToUnixTimeSeconds()}:F>";
        var discordUpdatedAtTimestamp = adminEntity.UpdatedAtUtc != null
            ? $"<t:{((DateTimeOffset)adminEntity.UpdatedAtUtc.Value).ToUnixTimeSeconds()}:F>"
            : "`N/A`";
        var statusIcon = adminEntity.IsActive ? activeIcon : inactiveIcon;

        var bodyText = $"""
                        **User:** {discordUser.Mention} (`{discordUser.Id}`)
                        **Role:** `{adminEntity.Role.Name}` *(Weight: {adminEntity.Role.HierarchyWeight})*
                        **Status:** {statusIcon} {(adminEntity.IsActive ? "`Active`" : "`Disabled`")}
                        **System Managed:** {(adminEntity.IsSystemManaged ? "`Yes` (Protected)" : "`No`")}
                        **Created At:** {discordCreatedAtTimestamp}
                        **Updated At:** {discordUpdatedAtTimestamp}
                        """;

        return CreateMessageWithAction("User Administration", bodyText, appendComponents);
    }

    public SelectMenuBuilder GetActionMenuBuilder(string customId, SystemAdministrators targetAdmin,
        SystemAdministrators requestingAdmin)
    {
        var menuBuilder = new SelectMenuBuilder().WithCustomId(customId)
            .WithPlaceholder("Select an administrative action...");
        var isSelf = targetAdmin.DiscordUserId == requestingAdmin.DiscordUserId;

        if (isSelf || requestingAdmin.Role.HierarchyWeight <= targetAdmin.Role.HierarchyWeight ||
            targetAdmin.IsSystemManaged)
        {
            menuBuilder.WithPlaceholder("No actions available")
                .AddOption("Protected / Insufficient permissions", "none", "Account is immutable.")
                .WithDisabled(true);
            return menuBuilder;
        }

        foreach (BotAdminAction action in Enum.GetValues(typeof(BotAdminAction)))
        {
            var optionInfo = action.GetDiscordOptionInfo();
            var emote = optionInfo?.Emote is { } emoteName ? emoteCache.GetEmote(emoteName) : null;
            menuBuilder.AddOption(label: optionInfo?.Label ?? action.ToString(), value: action.ToString(),
                description: optionInfo?.Description, emote: emote);
        }

        if (menuBuilder.Options.Count == 0)
        {
            menuBuilder.WithPlaceholder("No actions available")
                .AddOption("Insufficient permissions", "none", "You cannot manage this user.").WithDisabled(true);
        }

        return menuBuilder;
    }

    public SelectMenuBuilder GetSystemRoleMenuBuilder(string customId, int currentRoleId,
        List<SystemRoles> assignableRoles)
    {
        var menuBuilder = new SelectMenuBuilder()
            .WithCustomId(customId)
            .WithPlaceholder("Select new system role...");

        foreach (var role in assignableRoles)
        {
            menuBuilder.AddOption(
                label: role.Name,
                value: role.Id.ToString(),
                description: $"Hierarchy weight: {role.HierarchyWeight}",
                isDefault: role.Id == currentRoleId);
        }

        return menuBuilder;
    }

    public SelectMenuBuilder GetStatusMenuBuilder(string customId, bool currentState)
    {
        var trueIcon = emoteCache.GetEmote("UI_ICON_USER_CHECK");
        var falseIcon = emoteCache.GetEmote("UI_ICON_USER_LOCK");

        return new SelectMenuBuilder().WithCustomId(customId).WithPlaceholder("Select administrator status...")
            .AddOption("Enable Administrator", "true", "Administrator can issue bot commands.",
                isDefault: currentState, emote: trueIcon)
            .AddOption("Disable Administrator", "false", "Administrator is blocked from bot interaction.",
                isDefault: !currentState, emote: falseIcon);
    }

    private MessageComponent CreateMessageWithAction(string header, string body,
        Action<ContainerBuilder>? appendComponents)
    {
        var layout = new StandardLayout(emoteCache, client)
            .Create(header)
            .AddSection(
                new TextSection(body)
            );

        if (appendComponents != null)
        {
            layout.AddSection(
                new CbActionSection(appendComponents)
            );
        }

        return layout.Build();
    }
}
