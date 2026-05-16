using Application.Discord.Panels.Core;
using Application.Discord.Panels.ClientPanel.Payloads;
using Application.Discord.Panels.ClientPanel.States;
using Application.Repositories;

namespace Application.Discord.Panels.ClientPanel.Actions;

/// <summary>
/// Opens the known delivery targets screen for the selected API client.
/// </summary>
public sealed class OpenTargetsAction(
    IIntegrationClientRepository clientRepository,
    IKnownDeliveryTargetRepository targetRepository) : IPanelActionHandler
{
    /// <inheritdoc />
    public string Action => ClientPanelActionIds.OpenTargets;

    /// <inheritdoc />
    public async Task<PanelActionResult> ExecuteAsync(ConfigPanelContext context, PanelInteraction interaction)
    {
        var payload = ClientEntityPayload.FromContext(context);

        var client = await clientRepository.GetByIdAsync(payload.ClientId)
            ?? throw new InvalidOperationException("API client not found.");

        var targets = await targetRepository.GetAllByClientIdAsync(client.Id);

        return new UpdatePanelResult
        {
            State = new ClientTargetsState
            {
                Client = client,
                Targets = targets
            }
        };
    }
}
