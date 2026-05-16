using Application.Discord.Panels.Core;
using Application.Discord.Panels.ClientPanel.Payloads;
using Application.Discord.Panels.ClientPanel.States;
using Application.Repositories;

namespace Application.Discord.Panels.ClientPanel.Actions;

/// <summary>
/// Permanently deletes the selected API client after confirmation.
/// </summary>
public sealed class ConfirmDeleteAction(IIntegrationClientRepository repository) : IPanelActionHandler
{
    /// <inheritdoc />
    public string Action => ClientPanelActionIds.DeleteSubmit;

    /// <inheritdoc />
    public async Task<PanelActionResult> ExecuteAsync(ConfigPanelContext context, PanelInteraction interaction)
    {
        var payload = ClientEntityPayload.FromContext(context);

        var client = await repository.GetByIdAsync(payload.ClientId)
            ?? throw new InvalidOperationException("API client not found.");

        await repository.DeleteAsync(client.Id);

        return new UpdatePanelResult
        {
            State = new ClientDeletedState(),
            ToastMessage = "🗑️ Client permanently deleted."
        };
    }
}
