using Application.Discord.Panels.Core;
using Application.Discord.Panels.ClientPanel.States;
using Application.Repositories;

namespace Application.Discord.Panels.ClientPanel.Actions;

/// <summary>
/// Opens the API key regeneration confirmation screen for the selected API client.
/// </summary>
public sealed class PromptRenewAction(IIntegrationClientRepository repository) : IPanelActionHandler
{
    /// <inheritdoc />
    public string Action => ClientPanelActionIds.OpenRegenerateKey;

    /// <inheritdoc />
    public async Task<PanelActionResult> ExecuteAsync(ConfigPanelContext context, PanelInteraction interaction)
    {
        if (!long.TryParse(interaction.EntityId, out var clientId))
            throw new InvalidOperationException("Invalid client ID.");

        var client = await repository.GetByIdAsync(clientId)
            ?? throw new InvalidOperationException("API client not found.");

        return new UpdatePanelResult
        {
            State = new ClientWarningState
            {
                Client = client,
                WarningType = "Renew",
                WarningMessage = "Are you sure you want to regenerate the API key? The current key will be invalidated immediately, and all external systems will need to be updated."
            }
        };
    }
}
