using Discord;
using Discord.Interactions;
using Infrastructure.Exceptions;
using Infrastructure.Services.Zabbix;

namespace Infrastructure.Discord.SlashCommands.Commands.Zabbix.DirectMessages.Actions;

public sealed class ZabbixAcknowledgmentAction(
    ZabbixService zabbixService,
    ZabbixDirectMessagePanelRenderer panelRenderer)
{
    public async Task HandleSelectAsync(DiscordInteractionView module, long apiId,
        string eventId, string[] selectedValues)
    {
        await module.DeferInteractionAsync(ephemeral: true);
        var newAckState = bool.Parse(selectedValues[0]);

        var zabbixEvent = await zabbixService.GetEventDetailsAsync(apiId, eventId);
        if (zabbixEvent == null) throw new Exceptions.InteractionException("Event data missing.");

        var success = await zabbixService.AcknowledgeEventAsync(apiId, eventId, null, newAckState, false, zabbixEvent.Severity);
        if (!success) throw new Exceptions.InteractionException("Zabbix API rejected the request.");

        var panel = panelRenderer.CreatePanel(apiId, eventId, newAckState, zabbixEvent.Severity);

        await ((IComponentInteraction)module.Context.Interaction).ModifyOriginalResponseAsync(msg => msg.Components = panel);
    }
}
