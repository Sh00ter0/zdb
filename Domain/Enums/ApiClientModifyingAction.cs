using Domain.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Enums
{
    public enum ApiClientModifyingAction
    {
        [DiscordSelectOption(label: "Change client name", description: "Allows changing the client's name.", emote: "UI_ICON_PEN", requiredPermission: "api.clients.write")]
        ChangeName = 1,

        [DiscordSelectOption(label: "Enable or disable client", description: "Allows enabling or disabling the client.", emote: "UI_ICON_POWER", requiredPermission: "api.clients.write")]
        EnableOrDisableClient = 2,

        [DiscordSelectOption(label: "Renew Zabbix connection", description: "Allows renewing the Zabbix connection.", emote: "UI_ICON_PLUG", requiredPermission: "api.clients.write")]
        RenewZabbixConnection = 3,

        [DiscordSelectOption(label: "Display related targets", description: "Allows displaying related targets.", emote: "UI_ICON_LIST", requiredPermission: "api.clients.read")]
        DisplayRelatedTargets = 4,

        [DiscordSelectOption(label: "Renew API key", description: "Allows renewing the API key.", emote: "UI_ICON_KEY", requiredPermission: "api.clients.write")]
        RenewApiKey = 5,

        [DiscordSelectOption(label: "Remove client", description: "Allows removing the client.", emote: "UI_ICON_TRASH_RED", requiredPermission: "api.clients.write")]
        Remove = 6
    }
}
