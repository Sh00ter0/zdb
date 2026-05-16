using Application.Discord.Panels.Core;
using Domain.Entities;

namespace Application.Discord.Panels.ClientPanel.States;

/// <summary>
/// View state for the API client overview screen.
/// </summary>
public sealed record ClientOverviewState : IPanelViewState
{
    /// <summary>
    /// Gets the API client displayed by the overview screen.
    /// </summary>
    public required IntegrationClients Client { get; init; }

    /// <summary>
    /// Gets a newly generated plaintext API key shown once after regeneration.
    /// </summary>
    public string? NewGeneratedKey { get; init; }
}
