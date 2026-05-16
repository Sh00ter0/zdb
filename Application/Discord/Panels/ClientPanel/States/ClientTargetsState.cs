using Application.Discord.Panels.Core;
using Domain.Entities;

namespace Application.Discord.Panels.ClientPanel.States;

/// <summary>
/// View state for the API client's known delivery targets screen.
/// </summary>
public sealed record ClientTargetsState : IPanelViewState
{
    /// <summary>
    /// Gets the API client whose targets are displayed.
    /// </summary>
    public required IntegrationClients Client { get; init; }

    /// <summary>
    /// Gets the known delivery targets associated with the client.
    /// </summary>
    public required IReadOnlyList<KnownDeliveryTargets> Targets { get; init; }

    /// <summary>
    /// Gets whether the target list should be presented as empty.
    /// </summary>
    public bool ShowEmptyTargetsMessage => Targets == null || Targets.Count == 0;
}
