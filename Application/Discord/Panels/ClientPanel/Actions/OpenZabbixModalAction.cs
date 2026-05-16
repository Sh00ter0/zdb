using Application.Discord.Panels.Core;
using Application.Discord.Panels.ClientPanel.Payloads;
using System.Threading.Tasks;

namespace Application.Discord.Panels.ClientPanel.Actions;

/// <summary>
/// Requests the Zabbix connection update modal for the selected API client.
/// </summary>
public sealed class OpenZabbixModalAction : IPanelActionHandler
{
    /// <inheritdoc />
    public string Action => ClientPanelActionIds.OpenZabbix;

    /// <inheritdoc />
    public Task<PanelActionResult> ExecuteAsync(ConfigPanelContext context, PanelInteraction interaction)
    {
        var payload = ClientEntityPayload.FromContext(context);

        return Task.FromResult<PanelActionResult>(new OpenModalResult
        {
            ModalType = "UpdateZabbix",
            EntityId = payload.ClientId.ToString()
        });
    }
}
