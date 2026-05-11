using Client.Enums;
using System;

namespace Client.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public class DiscordSelectOptionAttribute : Attribute
    {
        public string Label { get; }
        public string? Description { get; }
        public string? Emote { get; }
        public string? RequiredPermission { get; }

        public DiscordSelectOptionAttribute(string label, string? description = null, string? emote = null, string? requiredPermission = null)
        {
            Label = label;
            Description = description;
            Emote = emote;
            RequiredPermission = requiredPermission;
        }
    }
}
