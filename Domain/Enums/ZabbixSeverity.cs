using Domain.Attributes;

namespace Domain.Enums
{
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
}
