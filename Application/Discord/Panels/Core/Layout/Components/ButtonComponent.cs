namespace Application.Discord.Panels.Core.Layout;

/// <summary>
/// Describes a Discord button without depending on Discord.NET builder types.
/// </summary>
/// <param name="Label">The visible button label.</param>
/// <param name="Action">The final panel action triggered by the button.</param>
/// <param name="Style">The visual button style.</param>
/// <param name="EmoteName">The optional configured emote name to render inside the button.</param>
/// <param name="Disabled">Whether the button should be disabled.</param>
/// <param name="Url">The optional URL used when rendering a link button.</param>
public sealed record ButtonComponent(
    string Label,
    PanelActionDescriptor Action,
    ButtonStyleType Style = ButtonStyleType.Primary,
    string? EmoteName = null,
    bool Disabled = false,
    string? Url = null) : IUiComponent;

/// <summary>
/// Defines the supported declarative button styles.
/// </summary>
public enum ButtonStyleType
{
    /// <summary>
    /// The primary Discord button style.
    /// </summary>
    Primary,

    /// <summary>
    /// The secondary Discord button style.
    /// </summary>
    Secondary,

    /// <summary>
    /// The success Discord button style.
    /// </summary>
    Success,

    /// <summary>
    /// The danger Discord button style.
    /// </summary>
    Danger,

    /// <summary>
    /// The link Discord button style.
    /// </summary>
    Link
}
