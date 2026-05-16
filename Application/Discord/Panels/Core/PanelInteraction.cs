namespace Application.Discord.Panels.Core;

/// <summary>
/// Describes a decoded panel interaction independently of Discord.NET component types.
/// </summary>
public sealed class PanelInteraction
{
    /// <summary>
    /// Gets the logical panel identifier that should handle the interaction.
    /// </summary>
    public required string Panel { get; init; }

    /// <summary>
    /// Gets the final action identifier to dispatch within the panel.
    /// </summary>
    public required string Action { get; init; }

    /// <summary>
    /// Gets the primary domain entity identifier associated with the interaction.
    /// </summary>
    public string? EntityId { get; init; }

    /// <summary>
    /// Gets an optional secondary domain entity identifier for nested resources.
    /// </summary>
    public string? SubEntityId { get; init; }

    /// <summary>
    /// Gets the Discord user identifier bound to the interaction when required.
    /// </summary>
    public ulong UserId { get; init; }

    /// <summary>
    /// Gets optional compact metadata encoded into the interaction custom id.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; init; }
}
