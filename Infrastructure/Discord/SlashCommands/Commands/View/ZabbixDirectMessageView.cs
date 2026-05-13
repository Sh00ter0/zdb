using Discord.Interactions;
using Infrastructure.Attributes;
using Infrastructure.Models.Modals;

namespace Infrastructure.Discord.SlashCommands.Commands
{
    public class ZabbixDirectMessageView(ZabbixDirectMessageController controller) : InteractionModuleBase<AppInteractionContext>
    {
        // Buttons Router
        [ComponentInteraction("zabbix_btn:*:*:*", ignoreGroupNames: true)]
        public async Task HandleZabbixButtonAsync([RequireActiveApiClient] long apiId, string eventId, string actionId)
            => await controller.ProcessZabbixActionAsync(Context, apiId, eventId, actionId, null);

        // Select Menus Router
        [ComponentInteraction("zabbix_select:*:*:*", ignoreGroupNames: true)]
        public async Task HandleZabbixSelectAsync([RequireActiveApiClient] long apiId, string eventId, string actionId, string[] selectedValues)
            => await controller.ProcessZabbixActionAsync(Context, apiId, eventId, actionId, selectedValues);

        // Modals Router
        [ModalInteraction("zabbix_modal_comment:*:*", ignoreGroupNames: true)]
        public async Task HandleZabbixCommentModalAsync([RequireActiveApiClient] long apiId, string eventId, SingleInputModal modal)
            => await controller.HandleZabbixCommentModalAsync(Context, apiId, eventId, modal);
    }
}