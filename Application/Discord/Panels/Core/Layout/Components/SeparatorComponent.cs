namespace Application.Discord.Panels.Core.Layout;

/// <summary>
/// Describes spacing or a divider between Components V2 layout elements.
/// </summary>
/// <param name="Size">The spacing size used by the separator.</param>
/// <param name="IsDivider">Whether the separator should show a visible divider line.</param>
public sealed record SeparatorComponent(
    SeparatorSize Size = SeparatorSize.Large,
    bool IsDivider = true) : IUiComponent;

/// <summary>
/// Defines the supported declarative separator spacing sizes.
/// </summary>
public enum SeparatorSize
{
    /// <summary>
    /// Small separator spacing.
    /// </summary>
    Small,

    /// <summary>
    /// Large separator spacing.
    /// </summary>
    Large
}
