using Domain.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Enums
{
    public enum IsActive
    {
        [DiscordSelectOption(label: "Active", emote: "UI_ICON_ACTIVE")]
        True,

        [DiscordSelectOption(label: "Inactive", emote: "UI_ICON_INACTIVE")]
        False
    }
}
