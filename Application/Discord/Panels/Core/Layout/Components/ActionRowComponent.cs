namespace Application.Discord.Panels.Core.Layout;

/// <summary>
/// Describes a Discord action row containing interactive child components.
/// </summary>
/// <param name="Components">The ordered interactive components in the row.</param>
public sealed record ActionRowComponent(IReadOnlyList<IUiComponent> Components) : IUiComponent;
