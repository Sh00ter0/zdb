namespace Application.Discord.Panels.Core.Layout;

/// <summary>
/// Describes a Discord Components V2 container and its child layout components.
/// </summary>
/// <param name="Header">The optional header text rendered at the top of the container.</param>
/// <param name="Components">The ordered child components inside the container.</param>
/// <param name="AccentColor">The optional Discord accent color as an RGB integer.</param>
/// <param name="UseBotThumbnail">Whether the container header should use the bot avatar thumbnail.</param>
/// <param name="IncludeFooter">Whether the standard application footer should be appended.</param>
/// <param name="FooterSeparatorSize">The separator spacing used before the standard footer.</param>
/// <param name="FooterSeparatorDivider">Whether the footer separator should render as a visible divider.</param>
public sealed record ContainerComponent(
    string? Header,
    IReadOnlyList<IUiComponent> Components,
    uint? AccentColor = null,
    bool UseBotThumbnail = true,
    bool IncludeFooter = true,
    SeparatorSize FooterSeparatorSize = SeparatorSize.Large,
    bool FooterSeparatorDivider = true) : IUiComponent;
