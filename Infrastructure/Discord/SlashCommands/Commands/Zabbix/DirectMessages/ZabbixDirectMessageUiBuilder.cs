using Application.Services.Discord;
using Discord;
using Discord.WebSocket;
using Domain.Enums;
using Infrastructure.Extensions;
using Infrastructure.Views.Components;
using Infrastructure.Views.Layouts;

namespace Infrastructure.Discord.SlashCommands.Commands.Zabbix.DirectMessages;

public sealed class ZabbixDirectMessageUiBuilder(DiscordSocketClient client, IDiscordEmoteService emoteCache)
{
    public SelectMenuBuilder GetAckMenuBuilder(string customId, bool currentState)
    {
        var trueIcon = emoteCache.GetEmote("UI_ICON_CHECK");
        var falseIcon = emoteCache.GetEmote("UI_ICON_X");
        return new SelectMenuBuilder().WithCustomId(customId).WithPlaceholder("Acknowledgment Status...")
            .AddOption("Acknowledged", "true", "Mark event as acknowledged", isDefault: currentState,
                emote: trueIcon)
            .AddOption("Not Acknowledged", "false", "Leave event unacknowledged", isDefault: !currentState,
                emote: falseIcon);
    }

    public SelectMenuBuilder GetSeverityMenuBuilder(string customId, int currentSeverity)
    {
        var sevMenu = new SelectMenuBuilder().WithCustomId(customId).WithPlaceholder("Update Severity...");
        foreach (ZabbixSeverity severity in Enum.GetValues(typeof(ZabbixSeverity)))
        {
            var optionInfo = severity.GetDiscordOptionInfo();
            if (optionInfo == null) continue;
            var emote = optionInfo.Emote is { } emoteName ? emoteCache.GetEmote(emoteName) : null;
            sevMenu.AddOption(label: optionInfo.Label, value: ((int)severity).ToString(),
                description: optionInfo.Description, emote: emote,
                isDefault: (int)severity == currentSeverity);
        }

        return sevMenu;
    }

    public MessageComponent CreateManagementPanel(SelectMenuBuilder ackMenu, SelectMenuBuilder sevMenu,
        ButtonBuilder commentBtn)
    {
        var layout = new StandardLayout(emoteCache, client)
            .Create("Manage Event")
            .AddSection(
                new ActionSection([ackMenu, sevMenu, commentBtn])
            );

        return layout.Build();
    }

    public Modal CreateCommentModal(string customId)
    {
        return new ModalBuilder().WithCustomId(customId)
            .WithTitle("Add Comment")
            .AddTextInput("Comment", "comment_text", TextInputStyle.Paragraph, "Enter your comment here...", required: true)
            .Build();
    }
}
