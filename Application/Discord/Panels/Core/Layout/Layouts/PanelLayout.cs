namespace Application.Discord.Panels.Core.Layout;

/// <summary>
/// Root declarative layout returned by panel layout builders.
/// </summary>
public sealed record PanelLayout
{
    /// <summary>
    /// Gets the top-level components that should be mapped to Discord message components.
    /// </summary>
    public IReadOnlyList<IUiComponent> Components { get; init; } = [];
}
