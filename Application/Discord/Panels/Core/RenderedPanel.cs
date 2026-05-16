using Discord;
using Application.Discord.Panels.Core.Layout;

namespace Application.Discord.Panels.Core;

/// <summary>
/// Represents the transport-ready output produced by a panel view renderer.
/// </summary>
public sealed class RenderedPanel
{
    /// <summary>
    /// Gets optional plain message content rendered above or beside Discord components.
    /// </summary>
    public string? Content { get; init; }

    /// <summary>
    /// Gets optional legacy Discord embeds for views that still render embed-based messages.
    /// </summary>
    public Embed[]? Embeds { get; init; }

    /// <summary>
    /// Gets optional prebuilt Discord.NET components for legacy renderers.
    /// </summary>
    public MessageComponent? Components { get; init; }

    /// <summary>
    /// Gets the declarative panel layout used by the current componentized rendering pipeline.
    /// </summary>
    public PanelLayout? Layout { get; init; }
}
