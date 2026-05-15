using Application.Common.Constants;
using Application.Services.Discord;
using Discord;
using Infrastructure.Exceptions;
using Infrastructure.Models.Modals;
using Infrastructure.Services.Zabbix;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Discord.SlashCommands.Commands.Controllers.Zabbix
{
    public class ZabbixDirectMessageController(
        ILogger<ZabbixDirectMessageController> logger,
        IDiscordUiService discordUiService,
        ZabbixService zabbixService)
    {
        public async Task ProcessZabbixActionAsync(AppInteractionContext context, long apiId, string eventId, string actionId, string[]? selectedValues)
        {
            var zabbixEvent = await zabbixService.GetEventDetailsAsync(apiId, eventId);
            if (zabbixEvent == null && actionId != DiscordComponentActions.Manage) throw new UserVisibleException("Event data missing.");
            
            bool currentAckState = zabbixEvent?.Acknowledged == 1;
            int currentSev = zabbixEvent?.Severity ?? 0;

            string action = actionId;
            if (selectedValues?.Length > 0)
            {
                action = actionId == DiscordComponentActions.AckActionPrefix 
                    ? $"{DiscordComponentActions.AckActionPrefix}_{selectedValues[0]}" 
                    : $"{DiscordComponentActions.SevActionPrefix}_{selectedValues[0]}";
            }

            switch (action)
            {
                case DiscordComponentActions.Manage:
                    await context.Interaction.DeferAsync(ephemeral: true);
                    await context.Interaction.FollowupAsync(components: BuildPanel(apiId, eventId, currentAckState, currentSev), ephemeral: true, flags: MessageFlags.ComponentsV2);
                    break;

                case DiscordComponentActions.AckTrue:
                case DiscordComponentActions.AckFalse:
                    await context.Interaction.DeferAsync(ephemeral: true);
                    bool newAckState = action == DiscordComponentActions.AckTrue;
                    if (!await zabbixService.AcknowledgeEventAsync(apiId, eventId, null, newAckState, false, currentSev)) throw new UserVisibleException("Zabbix API rejected the request.");
                    await ((IComponentInteraction)context.Interaction).ModifyOriginalResponseAsync(msg => msg.Components = BuildPanel(apiId, eventId, newAckState, currentSev));
                    break;

                case string s when s.StartsWith($"{DiscordComponentActions.SevActionPrefix}_"):
                    await context.Interaction.DeferAsync(ephemeral: true);
                    int newSev = int.Parse(s[(DiscordComponentActions.SevActionPrefix.Length + 1)..]); 
                    if (!await zabbixService.AcknowledgeEventAsync(apiId, eventId, null, currentAckState, false, newSev)) throw new UserVisibleException("Zabbix API rejected the request.");
                    await ((IComponentInteraction)context.Interaction).ModifyOriginalResponseAsync(msg => msg.Components = BuildPanel(apiId, eventId, currentAckState, newSev));
                    break;

                case DiscordComponentActions.Comment:
                    await context.Interaction.RespondWithModalAsync(discordUiService.CreateSingleInputModal($"zabbix_modal_comment:{apiId}:{eventId}", "Add Comment", "Comment", "Enter your comment...", 500, isParagraph: true));
                    break;
            }
        }

        public async Task HandleZabbixCommentModalAsync(AppInteractionContext context, long apiId, string eventId, SingleInputModal modal)
        {
            await context.Interaction.DeferAsync(ephemeral: true);
            var zabbixEvent = await zabbixService.GetEventDetailsAsync(apiId, eventId);

            if (await zabbixService.AcknowledgeEventAsync(apiId, eventId, modal.Input1, zabbixEvent?.Acknowledged == 1, false, zabbixEvent?.Severity ?? 0))
            {
                await context.Interaction.FollowupAsync($"Comment added to event `{eventId}`.", ephemeral: true);
                logger.LogInformation("Added comment to Zabbix event {EventId}", eventId);
            }
            else throw new UserVisibleException("Zabbix API rejected the request.");
        }

        private MessageComponent BuildPanel(long apiId, string eventId, bool ackState, int sev)
        {
            return discordUiService.CreateZabbixManagementPanel(eventId,
                discordUiService.GetZabbixAckMenuBuilder($"zabbix_select:{apiId}:{eventId}:{DiscordComponentActions.AckActionPrefix}", ackState),
                discordUiService.GetZabbixSeverityMenuBuilder($"zabbix_select:{apiId}:{eventId}:{DiscordComponentActions.SevActionPrefix}", sev),
                new ButtonBuilder().WithCustomId($"zabbix_btn:{apiId}:{eventId}:{DiscordComponentActions.Comment}").WithLabel("Add Comment").WithStyle(ButtonStyle.Primary).WithEmote(new Emoji("💬")));
        }
    }
}