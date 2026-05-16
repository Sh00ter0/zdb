namespace Application.Discord.Panels.Core.Layout;

/// <summary>
/// Describes a Discord text display component.
/// </summary>
/// <param name="Content">The Markdown-capable text content to render.</param>
public sealed record TextComponent(string Content) : IUiComponent;
