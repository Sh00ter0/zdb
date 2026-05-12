using Domain.Attributes;

namespace Domain.Enums
{
    public enum AllowedTargetModifyingAction
    {
        [DiscordSelectOption(label: "Change display name", description: "Allows changing the target’s display name.", emote: "UI_ICON_PEN", requiredPermission: "api.knownTargets.write")]
        ChangeFriendlyName = 1,

        [DiscordSelectOption(label: "Toggle auto-publish", description: "Enable or disable auto-publishing for announcements.", emote: "UI_ICON_PAPER_PLANE", requiredPermission: "api.knownTargets.write")]
        ChangeCrosspostMode = 2,

        [DiscordSelectOption(label: "Synchronize data", description: "Force a resync with Discord's current data.", emote: "UI_ICON_REFRESH", requiredPermission: "api.knownTargets.write")]
        SynchronizeTargetData = 3,

        [DiscordSelectOption(label: "Remove target", description: "Permanently removes this target from the database.", emote: "UI_ICON_TRASH_RED", requiredPermission: "api.knownTargets.write")]
        Remove = 4
    }
}
