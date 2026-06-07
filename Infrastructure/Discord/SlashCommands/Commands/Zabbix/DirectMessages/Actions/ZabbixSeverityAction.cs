using Discord;
using Discord.Interactions;
using Infrastructure.Exceptions;
using Infrastructure.Services.Zabbix;

namespace Infrastructure.Discord.SlashCommands.Commands.Zabbix.DirectMessages.Actions;

public sealed class ZabbixSeverityAction(
    ZabbixService zabbixService,
    ZabbixDirectMessagePanelRenderer panelRenderer)
{
    public async Task HandleSelectAsync(DiscordInteractionView module, long apiId,
        string eventId, string[] selectedValues)
    {
        await module.DeferInteractionAsync(ephemeral: true);
        var newSeverityValue = int.Parse(selectedValues[0]);

        var zabbixEvent = await zabbixService.GetEventDetailsAsync(apiId, eventId);
        if (zabbixEvent == null) throw new UserVisibleException("The event does not exist on the server.");

        var currentAckState = zabbixEvent.Acknowledged == 1;

        var success = await zabbixService.AcknowledgeEventAsync(apiId, eventId, null, currentAckState, false, newSeverityValue);
        if (!success) throw new UserVisibleException("Zabbix API rejected the request.");

        var panel = panelRenderer.CreatePanel(apiId, eventId, currentAckState, newSeverityValue);

        await ((IComponentInteraction)module.Context.Interaction).ModifyOriginalResponseAsync(msg => msg.Components = panel);
    }
}
