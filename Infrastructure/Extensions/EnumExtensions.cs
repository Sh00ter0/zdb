using Domain.Attributes;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Infrastructure.Extensions
{
    public static class EnumExtensions
    {
        public static DiscordSelectOptionAttribute? GetDiscordOptionInfo(this Enum enumValue)
        {
            var enumType = enumValue.GetType();
            var memberInfo = enumType.GetMember(enumValue.ToString()).FirstOrDefault();

            return memberInfo?.GetCustomAttribute<DiscordSelectOptionAttribute>();
        }

        public static string GetDiscordLabel(this Enum enumValue)
        {
            var discordAttr = GetDiscordOptionInfo(enumValue);

            return !string.IsNullOrEmpty(discordAttr?.Label)
                ? discordAttr.Label
                : enumValue.GetDisplayName();
        }

        public static string? GetDiscordDescription(this Enum enumValue)
        {
            return GetDiscordOptionInfo(enumValue)?.Description;
        }

        public static string? GetDiscordEmote(this Enum enumValue)
        {
            return GetDiscordOptionInfo(enumValue)?.Emote;
        }

        private static DisplayAttribute? GetDisplayAttribute(Enum enumValue)
        {
            var enumType = enumValue.GetType();
            var memberInfo = enumType.GetMember(enumValue.ToString()).FirstOrDefault();

            return memberInfo?.GetCustomAttribute<DisplayAttribute>();
        }

        public static string GetDisplayName(this Enum enumValue)
        {
            var displayAttribute = GetDisplayAttribute(enumValue);

            return !string.IsNullOrEmpty(displayAttribute?.Name)
                ? displayAttribute.Name
                : enumValue.ToString();
        }

        public static string? GetDisplayDescription(this Enum enumValue)
        {
            return GetDisplayAttribute(enumValue)?.Description;
        }

        public static string? GetDisplayShortName(this Enum enumValue)
        {
            return GetDisplayAttribute(enumValue)?.ShortName;
        }

        public static string? GetDisplayGroupName(this Enum enumValue)
        {
            return GetDisplayAttribute(enumValue)?.GroupName;
        }
    }
}
