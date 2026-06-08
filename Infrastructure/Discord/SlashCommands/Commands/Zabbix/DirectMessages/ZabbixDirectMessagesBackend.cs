using Discord.Interactions;
using Infrastructure.Discord.SlashCommands.Commands.Zabbix.DirectMessages.Actions;
using Infrastructure.Models.Modals;

namespace Infrastructure.Discord.SlashCommands.Commands.Zabbix.DirectMessages;

public sealed class ZabbixDirectMessagesBackend(
    ZabbixManageEventAction manageEventAction,
    ZabbixAcknowledgmentAction acknowledgmentAction,
    ZabbixSeverityAction severityAction,
    ZabbixCommentAction commentAction)
{
    public Task HandleManageButtonAsync(DiscordInteractionView module, long apiId,
        string eventId)
    {
        return manageEventAction.ExecuteAsync(module, apiId, eventId);
    }

    public Task HandleZabbixAckSelectAsync(DiscordInteractionView module, long apiId,
        string eventId, string[] selectedValues)
    {
        return acknowledgmentAction.HandleSelectAsync(module, apiId, eventId, selectedValues);
    }

    public Task HandleZabbixSevSelectAsync(DiscordInteractionView module, long apiId,
        string eventId, string[] selectedValues)
    {
        return severityAction.HandleSelectAsync(module, apiId, eventId, selectedValues);
    }

    public Task HandleZabbixCommentBtnAsync(DiscordInteractionView module, long apiId,
        string eventId)
    {
        return commentAction.ShowModalAsync(module, apiId, eventId);
    }

    public Task HandleActionModalAsync(DiscordInteractionView module, long apiId,
        string eventId, ZabbixCommentModal modalData)
    {
        return commentAction.HandleModalAsync(module, apiId, eventId, modalData);
    }
}
