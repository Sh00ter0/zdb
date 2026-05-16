using Application.Discord.Panels.ClientPanel.States;
using Application.Discord.Panels.Core;
using Application.Repositories;

namespace Application.Discord.Panels.ClientPanel.Actions;

/// <summary>
/// Returns the client panel to the overview screen for the selected API client.
/// </summary>
public sealed class BackToClientOverviewAction(IIntegrationClientRepository repository) : IPanelActionHandler
{
    /// <inheritdoc />
    public string Action => ClientPanelActionIds.Back;

    /// <inheritdoc />
    public async Task<PanelActionResult> ExecuteAsync(ConfigPanelContext context, PanelInteraction interaction)
    {
        if (!long.TryParse(interaction.EntityId, out var clientId))
            throw new InvalidOperationException("Invalid client ID.");

        var client = await repository.GetByIdAsync(clientId)
            ?? throw new InvalidOperationException("API client not found.");

        return new UpdatePanelResult
        {
            State = new ClientOverviewState
            {
                Client = client
            }
        };
    }
}
