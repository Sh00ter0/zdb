using Discord.Interactions;

namespace Application.Discord.Panels.Core;

/// <summary>
/// Carries request-scoped data required by panel state builders and action handlers.
/// </summary>
public sealed class ConfigPanelContext
{
    /// <summary>
    /// Gets the Discord interaction context for the current panel request.
    /// </summary>
    public required SocketInteractionContext Context { get; init; }

    /// <summary>
    /// Gets the scoped service provider associated with the interaction.
    /// </summary>
    public required IServiceProvider Services { get; init; }

    /// <summary>
    /// Gets the Discord user identifier that initiated the interaction.
    /// </summary>
    public required ulong UserId { get; init; }

    /// <summary>
    /// Gets the primary domain entity identifier for the panel request.
    /// </summary>
    public required string? EntityId { get; init; }

    /// <summary>
    /// Gets raw selected values or modal values supplied by Discord for data-bearing interactions.
    /// </summary>
    public string[]? RawInteractionData { get; init; }
}
