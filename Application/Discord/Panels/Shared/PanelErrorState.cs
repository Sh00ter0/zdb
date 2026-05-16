using Application.Discord.Panels.Core;

namespace Application.Discord.Panels.Shared.States;

/// <summary>
/// View state used to render a recoverable panel error.
/// </summary>
public sealed record PanelErrorState : IPanelViewState
{
    /// <summary>
    /// Gets the safe error message displayed to the user.
    /// </summary>
    public required string ErrorMessage { get; init; }

    /// <summary>
    /// Gets the short reference identifier generated for the error.
    /// </summary>
    public required string ReferenceId { get; init; }
}
