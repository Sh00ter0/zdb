using Client.Attributes;
using Discord.Interactions;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Client.Enums
{
    public enum TextChannelType
    {
        [DiscordSelectOption(label: "Unknown")]
        Unknown = 0,

        [DiscordSelectOption(label: "Direct Message", description: "Direct message channel")]
        DirectMessage = 1,

        [DiscordSelectOption(label: "Text Channel", description: "Text channel")]
        GuildTextChannel = 2,

        [DiscordSelectOption(label: "Announcement Channel", description: "Announcement channel")]
        GuildAnnouncementChannel = 3,

        [DiscordSelectOption(label: "Public Thread Channel", description: "Public thread channel")]
        GuildPublicThreadChannel = 4,

        [DiscordSelectOption(label: "Private Thread Channel", description: "Private thread channel")]
        GuildPrivateThreadChannel = 5,

        [DiscordSelectOption(label: "Forum Thread Channel", description: "Forum thread channel")]
        GuildForumThreadChannel = 6,

        [DiscordSelectOption(label: "Voice Text Channel", description: "Voice text channel")]
        GuildVoiceTextChannel = 7,

        [DiscordSelectOption(label: "Stage Voice Text Channel", description: "Stage voice text channel")]
        GuildStageVoiceTextChannel = 8
    }

    public enum ZabbixSeverity
    {
        [DiscordSelectOption(label: "Not classified", description: "No severity level assigned.", emote: "UI_ICON_SEVERITY_NOT_CLASSIFIED")]
        NotClassified = 0,
        [DiscordSelectOption(label: "Information", description: "Information severity level.", emote: "UI_ICON_SEVERITY_INFORMATION")]
        Information = 1,
        [DiscordSelectOption(label: "Warning", description: "Warning severity level.", emote: "UI_ICON_SEVERITY_WARNING")]
        Warning = 2,
        [DiscordSelectOption(label: "Average", description: "Average severity level.", emote: "UI_ICON_SEVERITY_AVERAGE")]
        Average = 3,
        [DiscordSelectOption(label: "High", description: "High severity level.", emote: "UI_ICON_SEVERITY_HIGH")]
        High = 4,
        [DiscordSelectOption(label: "Disaster", description: "Disaster severity level.", emote: "UI_ICON_SEVERITY_DISASTER")]
        Disaster = 5
    }

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

    public enum BotAdminAction
    {
        [DiscordSelectOption(label: "Change user role", description: "Allows changing the user's role.", emote: "UI_ICON_SHIELD_CHECK", requiredPermission: "system.admins.write")]
        ChangeUserRole = 2,

        [DiscordSelectOption(label: "Enable or disable user", description: "Allows enabling or disabling the user.", emote: "UI_ICON_USER_LOCK", requiredPermission: "system.admins.write")]
        EnableOrDisableUser = 3
    }

    public enum IsActive
    {
        [DiscordSelectOption(label: "Active", emote: "UI_ICON_ACTIVE")]
        True,

        [DiscordSelectOption(label: "Inactive", emote: "UI_ICON_INACTIVE")]
        False
    }
}