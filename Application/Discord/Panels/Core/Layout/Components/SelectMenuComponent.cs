namespace Application.Discord.Panels.Core.Layout;

/// <summary>
/// Describes a Discord select menu in the declarative panel layout.
/// </summary>
/// <param name="Placeholder">The placeholder text shown by Discord before selection.</param>
/// <param name="Action">The action associated with the menu when options carry raw values.</param>
/// <param name="Options">The options displayed by the select menu.</param>
/// <param name="Disabled">Whether the select menu should be disabled.</param>
/// <param name="MinValues">The minimum number of values Discord should require.</param>
/// <param name="MaxValues">The maximum number of values Discord should allow.</param>
public sealed record SelectMenuComponent(
    string Placeholder,
    PanelActionDescriptor Action,
    IReadOnlyList<SelectMenuOptionComponent> Options,
    bool Disabled = false,
    int MinValues = 1,
    int MaxValues = 1) : IUiComponent;

/// <summary>
/// Describes one option inside a declarative select menu.
/// </summary>
public sealed record SelectMenuOptionComponent
{
    /// <summary>
    /// Gets the visible option label.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Gets the raw data value for data select menus.
    /// </summary>
    public string? Value { get; init; }

    /// <summary>
    /// Gets the final panel action for navigation select menus.
    /// </summary>
    public PanelActionDescriptor? Action { get; init; }

    /// <summary>
    /// Gets the optional option description shown by Discord.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the optional configured emote name displayed with the option.
    /// </summary>
    public string? EmoteName { get; init; }

    /// <summary>
    /// Gets whether Discord should render this option as selected by default.
    /// </summary>
    public bool IsDefault { get; init; }
}
