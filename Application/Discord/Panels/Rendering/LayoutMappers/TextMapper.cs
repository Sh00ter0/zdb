using Application.Discord.Panels.Core.Layout;
using Application.Services.Discord;
using Discord;
using System.Text.RegularExpressions;

namespace Application.Discord.Panels.Rendering.LayoutMappers;

/// <summary>
/// Maps layout text components into Discord text display builders.
/// </summary>
public sealed partial class TextMapper(IDiscordEmoteService emotes) : ILayoutComponentMapper
{
    /// <inheritdoc />
    public bool CanMap(IUiComponent component) => component is TextComponent;

    /// <inheritdoc />
    public object Map(IUiComponent component)
    {
        var text = (TextComponent)component;
        return new TextDisplayBuilder(ResolveEmotes(text.Content));
    }

    /// <summary>
    /// Replaces panel emote tokens with Discord emote mentions.
    /// </summary>
    /// <param name="content">The Markdown content that may contain emote tokens.</param>
    /// <returns>The content with known emote tokens resolved.</returns>
    private string ResolveEmotes(string content)
    {
        return EmoteTokenPattern().Replace(content, match =>
        {
            var emote = emotes.GetEmote(match.Groups["name"].Value);
            return emote?.ToString() ?? string.Empty;
        });
    }

    /// <summary>
    /// Matches panel emote tokens in the form <c>{emote:EMOTE_NAME}</c>.
    /// </summary>
    /// <returns>The compiled regular expression used to locate emote tokens.</returns>
    [GeneratedRegex(@"\{emote:(?<name>[A-Za-z0-9_]+)\}")]
    private static partial Regex EmoteTokenPattern();
}
