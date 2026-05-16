using Application.Discord.Panels.Core.Layout;
using Discord;
using Discord.WebSocket;
using LayoutSectionComponent = Application.Discord.Panels.Core.Layout.SectionComponent;

namespace Application.Discord.Panels.Rendering.LayoutMappers;

/// <summary>
/// Maps layout sections into Discord section or text display builders.
/// </summary>
public sealed class SectionMapper(
    TextMapper textMapper,
    DiscordSocketClient client) : ILayoutComponentMapper
{
    /// <inheritdoc />
    public bool CanMap(IUiComponent component) => component is LayoutSectionComponent;

    /// <inheritdoc />
    public object Map(IUiComponent component)
    {
        var section = (LayoutSectionComponent)component;
        var textDisplays = section.Texts
            .Select(x => (TextDisplayBuilder)textMapper.Map(x))
            .ToArray();

        var thumbnailUrl = section.UseBotThumbnail
            ? GetBotAvatarUrl()
            : section.ThumbnailUrl;

        if (string.IsNullOrWhiteSpace(thumbnailUrl))
            return textDisplays.Length == 1 ? textDisplays[0] : new SectionBuilder().WithComponents(textDisplays);

        return new SectionBuilder()
            .WithComponents(textDisplays)
            .WithAccessory(new ThumbnailBuilder(thumbnailUrl));
    }

    /// <summary>
    /// Resolves the current bot avatar URL for sections that request a bot thumbnail.
    /// </summary>
    /// <returns>The display or default bot avatar URL, or <see langword="null" /> when unavailable.</returns>
    private string? GetBotAvatarUrl() =>
        client.CurrentUser?.GetDisplayAvatarUrl()
        ?? client.CurrentUser?.GetDefaultAvatarUrl();
}
