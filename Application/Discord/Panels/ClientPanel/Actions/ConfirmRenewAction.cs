using Application.Discord.Panels.Core;
using Application.Discord.Panels.ClientPanel.Payloads;
using Application.Discord.Panels.ClientPanel.States;
using Application.Repositories;
using Application.Services.API;

namespace Application.Discord.Panels.ClientPanel.Actions;

/// <summary>
/// Regenerates an API key after the user confirms the warning screen.
/// </summary>
public sealed class ConfirmRenewAction(
    IIntegrationClientRepository repository,
    IApiSecurityStore apiSecurityStore) : IPanelActionHandler
{
    /// <inheritdoc />
    public string Action => ClientPanelActionIds.RenewSubmit;

    /// <inheritdoc />
    public async Task<PanelActionResult> ExecuteAsync(ConfigPanelContext context, PanelInteraction interaction)
    {
        var payload = ClientEntityPayload.FromContext(context);

        var client = await repository.GetByIdAsync(payload.ClientId)
            ?? throw new InvalidOperationException("API client not found.");

        string newPlaintextKey = await apiSecurityStore.RenewApiKeyAsync(client.Id);

        var updatedClient = await repository.GetByIdAsync(payload.ClientId)
            ?? throw new InvalidOperationException("Failed to retrieve client after key renewal.");

        return new UpdatePanelResult
        {
            State = new ClientOverviewState
            {
                Client = updatedClient,
                NewGeneratedKey = newPlaintextKey
            },
            ToastMessage = "✅ API Key regenerated successfully."
        };
    }
}
