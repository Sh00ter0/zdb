using Application.Discord.Panels.Core;
using Domain.Entities;

namespace Application.Discord.Panels.ClientPanel.States;

/// <summary>
/// View state for confirmation screens that warn before sensitive client actions.
/// </summary>
public sealed record ClientWarningState : IPanelViewState
{
    /// <summary>
    /// Gets the API client affected by the warning action.
    /// </summary>
    public required IntegrationClients Client { get; init; }

    /// <summary>
    /// Gets the warning kind used by the layout builder to choose labels and styles.
    /// </summary>
    public required string WarningType { get; init; }

    /// <summary>
    /// Gets the warning message shown to the user.
    /// </summary>
    public required string WarningMessage { get; init; }
}
