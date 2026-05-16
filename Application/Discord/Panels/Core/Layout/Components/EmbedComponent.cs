namespace Application.Discord.Panels.Core.Layout;

/// <summary>
/// Describes a legacy Discord embed in the declarative layout model.
/// </summary>
/// <param name="Title">The optional embed title.</param>
/// <param name="Description">The optional embed description.</param>
/// <param name="Color">The optional embed color as an RGB integer.</param>
public sealed record EmbedComponent(
    string? Title,
    string? Description,
    uint? Color = null) : IUiComponent;
