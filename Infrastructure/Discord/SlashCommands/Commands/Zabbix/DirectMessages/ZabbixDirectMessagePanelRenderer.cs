using Discord;

namespace Infrastructure.Discord.SlashCommands.Commands.Zabbix.DirectMessages;

public sealed class ZabbixDirectMessagePanelRenderer(ZabbixDirectMessageUiBuilder uiBuilder)
{
    public MessageComponent CreatePanel(long apiId, string eventId, bool currentAckState, int currentSeverity)
    {
        var ackMenu = uiBuilder.GetAckMenuBuilder($"zabbix_select_ack:{apiId}:{eventId}", currentAckState);
        var sevMenu = uiBuilder.GetSeverityMenuBuilder($"zabbix_select_sev:{apiId}:{eventId}", currentSeverity);
        var commentBtn = new ButtonBuilder()
            .WithCustomId($"zabbix_btn_comment:{apiId}:{eventId}")
            .WithLabel("Add Comment")
            .WithStyle(ButtonStyle.Primary)
            .WithEmote(new Emoji("💬"));

        return uiBuilder.CreateManagementPanel(ackMenu, sevMenu, commentBtn);
    }
}
