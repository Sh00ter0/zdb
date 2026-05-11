using Domain.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Enums
{
    public enum BotAdminAction
    {
        [DiscordSelectOption(label: "Change user role", description: "Allows changing the user's role.", emote: "UI_ICON_SHIELD_CHECK", requiredPermission: "system.admins.write")]
        ChangeUserRole = 2,

        [DiscordSelectOption(label: "Enable or disable user", description: "Allows enabling or disabling the user.", emote: "UI_ICON_USER_LOCK", requiredPermission: "system.admins.write")]
        EnableOrDisableUser = 3
    }
}
