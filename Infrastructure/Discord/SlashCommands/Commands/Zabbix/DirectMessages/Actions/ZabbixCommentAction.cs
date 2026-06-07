using Discord.Interactions;
using Infrastructure.Exceptions;
using Infrastructure.Models.Modals;
using Infrastructure.Services.Zabbix;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Discord.SlashCommands.Commands.Zabbix.DirectMessages.Actions;

public sealed class ZabbixCommentAction(
    ZabbixService zabbixService,
    ZabbixDirectMessageUiBuilder uiBuilder,
    ILogger<ZabbixCommentAction> logger)
{
    public Task ShowModalAsync(DiscordInteractionView module, long apiId, string eventId)
    {
        var modal = uiBuilder.CreateCommentModal($"zabbix_modal_comment:{apiId}:{eventId}");
        return module.RespondWithModalInteractionAsync(modal);
    }

    public async Task HandleModalAsync(DiscordInteractionView module, long apiId,
        string eventId, ZabbixCommentModal modalData)
    {
        await module.DeferInteractionAsync(ephemeral: true);

        var comment = modalData.Comment;

        var zabbixEvent = await zabbixService.GetEventDetailsAsync(apiId, eventId);
        var currentAckState = zabbixEvent?.Acknowledged == 1;
        var currentSeverity = zabbixEvent?.Severity ?? 0;

        var success = await zabbixService.AcknowledgeEventAsync(apiId, eventId, comment, currentAckState, false, currentSeverity);

        if (success)
        {
            await module.FollowupInteractionAsync($"Comment added to event `{eventId}`.", ephemeral: true);
            logger.LogInformation("Successfully added comment to Zabbix event {EventId}", eventId);
        }
        else
        {
            throw new UserVisibleException("Zabbix API rejected the request.");
        }
    }
}
