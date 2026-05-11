using Domain.Attributes;

namespace Domain.Enums
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
}
