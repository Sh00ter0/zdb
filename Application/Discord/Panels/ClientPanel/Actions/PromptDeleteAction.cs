using Application.Discord.Panels.Core;
using Application.Discord.Panels.ClientPanel.Payloads;
using Application.Discord.Panels.ClientPanel.States;
using Application.Repositories;
using System;
using System.Threading.Tasks;

namespace Application.Discord.Panels.ClientPanel.Actions;

/// <summary>
/// Opens the destructive delete confirmation screen for the selected API client.
/// </summary>
public sealed class PromptDeleteAction(IIntegrationClientRepository repository) : IPanelActionHandler
{
    /// <inheritdoc />
    public string Action => ClientPanelActionIds.OpenDeleteWarning;

    /// <inheritdoc />
    public async Task<PanelActionResult> ExecuteAsync(ConfigPanelContext context, PanelInteraction interaction)
    {
        var payload = ClientEntityPayload.FromContext(context);

        var client = await repository.GetByIdAsync(payload.ClientId)
            ?? throw new InvalidOperationException("API client not found.");

        return new UpdatePanelResult
        {
            State = new ClientWarningState
            {
                Client = client,
                WarningType = "Delete",
                WarningMessage = "Are you sure you want to permanently delete this API client? All associated targets and configurations will be lost."
            }
        };
    }
}
