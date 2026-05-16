using Application.Discord.Panels.Core;
using Application.Discord.Panels.ClientPanel.Payloads;
using Application.Discord.Panels.ClientPanel.States;
using Application.Repositories;

namespace Application.Discord.Panels.ClientPanel.Actions;

/// <summary>
/// Applies the selected active or disabled status to an API client.
/// </summary>
public sealed class ToggleClientStatusAction(IIntegrationClientRepository repository) : IPanelActionHandler
{
    /// <inheritdoc />
    public string Action => ClientPanelActionIds.ToggleStatus;

    /// <inheritdoc />
    public async Task<PanelActionResult> ExecuteAsync(ConfigPanelContext context, PanelInteraction interaction)
    {
        var payload = ToggleClientStatusPayload.FromContext(context);

        var client = await repository.GetByIdAsync(payload.ClientId)
            ?? throw new InvalidOperationException("API client not found.");

        client.IsActive = payload.IsEnabled;
        await repository.UpdateAsync(client);

        return new UpdatePanelResult
        {
            State = new ClientOverviewState { Client = client },
            ToastMessage = payload.IsEnabled ? "✅ Client enabled successfully." : "🛑 Client disabled."
        };
    }
}
