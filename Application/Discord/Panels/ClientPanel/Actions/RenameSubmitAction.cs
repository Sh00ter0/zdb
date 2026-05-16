using Application.Discord.Panels.Core;
using Application.Discord.Panels.ClientPanel.Payloads;
using Application.Discord.Panels.ClientPanel.States;
using Application.Repositories;

namespace Application.Discord.Panels.ClientPanel.Actions;

/// <summary>
/// Handles the rename modal submission and updates the API client name.
/// </summary>
public sealed class RenameSubmitAction(IIntegrationClientRepository repository) : IPanelActionHandler
{
    /// <inheritdoc />
    public string Action => ClientPanelActionIds.RenameSubmit;

    /// <inheritdoc />
    public async Task<PanelActionResult> ExecuteAsync(ConfigPanelContext context, PanelInteraction interaction)
    {
        var payload = RenameClientPayload.FromContext(context);

        var client = await repository.GetByIdAsync(payload.ClientId)
            ?? throw new InvalidOperationException("API client not found.");

        client.Name = payload.NewName;
        await repository.UpdateAsync(client);

        return new UpdatePanelResult
        {
            State = new ClientOverviewState { Client = client },
            ToastMessage = "✅ Client name updated successfully."
        };
    }
}
