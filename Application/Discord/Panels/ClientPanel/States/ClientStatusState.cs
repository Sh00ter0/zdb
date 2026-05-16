using Application.Discord.Panels.Core;
using Domain.Entities;

namespace Application.Discord.Panels.ClientPanel.States;

/// <summary>
/// View state for the client status management screen.
/// </summary>
public sealed record ClientStatusState : IPanelViewState
{
    /// <summary>
    /// Gets the API client whose operational status is being managed.
    /// </summary>
    public required IntegrationClients Client { get; init; }
}
