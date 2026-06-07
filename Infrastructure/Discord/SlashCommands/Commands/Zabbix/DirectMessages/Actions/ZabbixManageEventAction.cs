using Discord;
using Discord.Interactions;
using Infrastructure.Exceptions;
using Infrastructure.Services.Zabbix;

namespace Infrastructure.Discord.SlashCommands.Commands.Zabbix.DirectMessages.Actions;

public sealed class ZabbixManageEventAction(
    ZabbixService zabbixService,
    ZabbixDirectMessagePanelRenderer panelRenderer)
{
    public async Task ExecuteAsync(DiscordInteractionView module, long apiId, string eventId)
    {
        await module.DeferInteractionAsync(ephemeral: true);

        var zabbixEvent = await zabbixService.GetEventDetailsAsync(apiId, eventId);
        if (zabbixEvent == null) throw new UserVisibleException("Failed to retrieve event details from the Zabbix server.");

        var currentAckState = zabbixEvent.Acknowledged == 1;
        var currentSeverity = zabbixEvent.Severity;

        var panel = panelRenderer.CreatePanel(apiId, eventId, currentAckState, currentSeverity);

        await module.FollowupInteractionAsync(components: panel, ephemeral: true, flags: MessageFlags.ComponentsV2);
    }
}
