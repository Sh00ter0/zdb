using Application.Services.Discord;
using Discord;
using Discord.WebSocket;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Extensions;
using Infrastructure.Views.Components;
using Infrastructure.Views.Layouts;

namespace Infrastructure.Discord.SlashCommands.Commands.Api.KnownTargets;

public sealed class KnownTargetUiBuilder(DiscordSocketClient client, IDiscordEmoteService emoteCache)
{
    public MessageComponent CreateOverviewContainer(string clientName, KnownDeliveryTargets target,
        Action<ContainerBuilder>? appendComponents = null)
    {
        var discordCreatedAtTimestamp = $"<t:{((DateTimeOffset)target.CreatedAtUtc).ToUnixTimeSeconds()}:F>";
        var discordUpdatedAtTimestamp = target.UpdatedAtUtc != null
            ? $"<t:{((DateTimeOffset)target.UpdatedAtUtc).ToUnixTimeSeconds()}:F>"
            : "`N/A`";
        var friendlyChannelType = target.ChannelType.GetDiscordLabel();
        var guildInfo = target.AssociatedGuildId.HasValue ? $"`{target.AssociatedGuildId.Value}`" : "`N/A`";

        var bodyText = $"""
                        **Name:** `{target.Name}`
                        **Target ID:** `{target.TargetId}`
                        **Type:** `{friendlyChannelType}`
                        **Associated Guild:** {guildInfo}
                        **Auto-Publish:** `{target.AutoCrosspost}`
                        **Created At:** {discordCreatedAtTimestamp}
                        **Updated At:** {discordUpdatedAtTimestamp}
                        """;

        return CreateMessageWithAction($"Manage Target: {clientName}", bodyText, appendComponents);
    }

    public SelectMenuBuilder GetManagementMenuBuilder(string customId, List<string> userPermissions)
    {
        var menuBuilder = new SelectMenuBuilder().WithCustomId(customId)
            .WithPlaceholder("Select an action to perform...");
        var isRoot = userPermissions.Contains("root");

        foreach (AllowedTargetModifyingAction action in Enum.GetValues(typeof(AllowedTargetModifyingAction)))
        {
            var optionInfo = action.GetDiscordOptionInfo();
            if (optionInfo == null) continue;

            if (!isRoot && optionInfo.RequiredPermission != null &&
                !userPermissions.Contains(optionInfo.RequiredPermission)) continue;

            var emote = optionInfo.Emote is { } emoteName ? emoteCache.GetEmote(emoteName) : null;
            menuBuilder.AddOption(label: optionInfo.Label, value: action.ToString(),
                description: optionInfo.Description, emote: emote);
        }

        if (menuBuilder.Options.Count == 0)
        {
            menuBuilder.WithPlaceholder("No actions available").AddOption("Insufficient permissions", "none",
                "You do not have permission to manage this.").WithDisabled(true);
        }

        return menuBuilder;
    }

    public SelectMenuBuilder GetCrosspostSelectMenuBuilder(string customId, bool currentState)
    {
        var trueIcon = emoteCache.GetEmote("UI_ICON_CHECK");
        var falseIcon = emoteCache.GetEmote("UI_ICON_X");
        return new SelectMenuBuilder().WithCustomId(customId).WithPlaceholder("Select auto-publish mode...")
            .AddOption("Enable Auto-Publish", "true", "Messages will be automatically published",
                isDefault: currentState, emote: trueIcon)
            .AddOption("Disable Auto-Publish", "false", "Messages will NOT be automatically published",
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
