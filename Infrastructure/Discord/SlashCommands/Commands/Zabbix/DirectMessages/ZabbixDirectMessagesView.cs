using Discord.Interactions;
using Infrastructure.Attributes;
using Infrastructure.Models.Modals;

namespace Infrastructure.Discord.SlashCommands.Commands.Zabbix.DirectMessages;

public sealed class ZabbixDirectMessagesView(ZabbixDirectMessagesBackend backend) : DiscordInteractionView
{
    [ComponentInteraction("btn_manage:*:*", ignoreGroupNames: true)]
    public Task HandleManageButton([RequireActiveApiClient] long apiId, string eventId)
    {
        return backend.HandleManageButtonAsync(this, apiId, eventId);
    }

    [ComponentInteraction("zabbix_select_ack:*:*", ignoreGroupNames: true)]
    public Task HandleZabbixAckSelect(long apiId, string eventId, string[] selectedValues)
    {
        return backend.HandleZabbixAckSelectAsync(this, apiId, eventId, selectedValues);
    }

    [ComponentInteraction("zabbix_select_sev:*:*", ignoreGroupNames: true)]
    public Task HandleZabbixSevSelect(long apiId, string eventId, string[] selectedValues)
    {
        return backend.HandleZabbixSevSelectAsync(this, apiId, eventId, selectedValues);
    }

    [ComponentInteraction("zabbix_btn_comment:*:*", ignoreGroupNames: true)]
    public Task HandleZabbixCommentBtn(long apiId, string eventId)
    {
        return backend.HandleZabbixCommentBtnAsync(this, apiId, eventId);
    }

    [ModalInteraction("zabbix_modal_comment:*:*", ignoreGroupNames: true)]
    public Task HandleActionModal([RequireActiveApiClient] long apiId, string eventId, ZabbixCommentModal modalData)
    {
        return backend.HandleActionModalAsync(this, apiId, eventId, modalData);
    }
}
