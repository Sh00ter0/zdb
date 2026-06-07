using Application.Services.Discord;
using Discord;
using Discord.WebSocket;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Extensions;
using Infrastructure.Views.Components;
using Infrastructure.Views.Layouts;

namespace Infrastructure.Discord.SlashCommands.Commands.Api.Clients;

public sealed class ApiClientUiBuilder(DiscordSocketClient client, IDiscordEmoteService emoteCache)
{
    public MessageComponent CreateOverviewContainer(IntegrationClients clientEntity,
        Action<ContainerBuilder>? appendComponents = null)
    {
        var bulbIconOn = emoteCache.GetEmote("UI_ICON_BULB_ON");
        var bulbIconOff = emoteCache.GetEmote("UI_ICON_BULB_OFF");
        var discordCreateTimestamp = $"<t:{((DateTimeOffset)clientEntity.CreatedAtUtc).ToUnixTimeSeconds()}:F>";
        var discordUpdateTimestamp = clientEntity.UpdatedAtUtc != null
            ? $"<t:{((DateTimeOffset)clientEntity.UpdatedAtUtc).ToUnixTimeSeconds()}:F>"
            : "`N/A`";
        var statusIcon = clientEntity.IsActive ? bulbIconOn + "`Active`" : bulbIconOff + "`Disabled`";
        var zabbixUrl = string.IsNullOrEmpty(clientEntity.ZabbixCredential?.ApiUrl)
            ? "`Not Configured`"
            : $"`{clientEntity.ZabbixCredential.ApiUrl}`";

        var bodyText = $"""
                        **Client Name:** `{clientEntity.Name}`
                        **Status:** {statusIcon}
                        **Key Preview:** `{clientEntity.KeyPreview}`
                        **Zabbix URL:** {zabbixUrl}
                        **Created At:** {discordCreateTimestamp}
                        **Updated At:** {discordUpdateTimestamp}
                        """;

        return CreateMessageWithAction("Manage API Client", bodyText, appendComponents);
    }

    public SelectMenuBuilder GetManagementMenuBuilder(string customId, List<string> userPermissions)
    {
        var menuBuilder = new SelectMenuBuilder().WithCustomId(customId)
            .WithPlaceholder("Select a client action to perform...");
        var isRoot = userPermissions.Contains("root");

        foreach (ApiClientModifyingAction action in Enum.GetValues(typeof(ApiClientModifyingAction)))
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

    public SelectMenuBuilder GetStatusSelectMenuBuilder(string customId, bool currentState)
    {
        var onIcon = emoteCache.GetEmote("UI_ICON_BULB_ON");
        var offIcon = emoteCache.GetEmote("UI_ICON_BULB_OFF");
        return new SelectMenuBuilder().WithCustomId(customId).WithPlaceholder("Select client status...")
            .AddOption("Enabled", "true", "Client is active and processing requests", isDefault: currentState,
                emote: onIcon)
            .AddOption("Disabled", "false", "Client is inactive and will reject requests", isDefault: !currentState,
                emote: offIcon);
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
