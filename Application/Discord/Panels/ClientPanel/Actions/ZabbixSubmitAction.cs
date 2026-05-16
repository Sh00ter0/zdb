using Application.Discord.Panels.Core;
using Application.Discord.Panels.ClientPanel.Payloads;
using Application.Discord.Panels.ClientPanel.States;
using Application.Repositories;

namespace Application.Discord.Panels.ClientPanel.Actions;

/// <summary>
/// Handles the Zabbix connection modal submission and updates stored connection data.
/// </summary>
public sealed class ZabbixSubmitAction(IIntegrationClientRepository repository) : IPanelActionHandler
{
    /// <inheritdoc />
    public string Action => ClientPanelActionIds.ZabbixSubmit;

    /// <inheritdoc />
    public async Task<PanelActionResult> ExecuteAsync(ConfigPanelContext context, PanelInteraction interaction)
    {
        var payload = UpdateZabbixPayload.FromContext(context);

        var client = await repository.GetByIdAsync(payload.ClientId)
            ?? throw new InvalidOperationException("API client not found.");

        client.ZabbixCredential!.ApiUrl = payload.ApiUrl;
        await repository.UpdateAsync(client);

        return new UpdatePanelResult
        {
            State = new ClientOverviewState { Client = client },
            ToastMessage = "✅ Zabbix connection updated."
        };
    }
}
