namespace Application.Discord.Panels.Core.Layout;

/// <summary>
/// Describes a Discord Components V2 section with text and an optional thumbnail.
/// </summary>
/// <param name="Texts">The text displays rendered in the section.</param>
/// <param name="ThumbnailUrl">The optional thumbnail URL used as a section accessory.</param>
/// <param name="UseBotThumbnail">Whether the section should use the bot avatar as its thumbnail.</param>
public sealed record SectionComponent(
    IReadOnlyList<TextComponent> Texts,
    string? ThumbnailUrl = null,
    bool UseBotThumbnail = false) : IUiComponent;
