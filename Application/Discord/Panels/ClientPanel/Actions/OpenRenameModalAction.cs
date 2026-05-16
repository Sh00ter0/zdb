using Application.Discord.Panels.Core;
using Application.Discord.Panels.ClientPanel.Payloads;

namespace Application.Discord.Panels.ClientPanel.Actions;

/// <summary>
/// Requests the rename modal for the selected API client.
/// </summary>
public sealed class OpenRenameModalAction : IPanelActionHandler
{
    /// <inheritdoc />
    public string Action => ClientPanelActionIds.OpenRename;

    /// <inheritdoc />
    public Task<PanelActionResult> ExecuteAsync(ConfigPanelContext context, PanelInteraction interaction)
    {
        var payload = ClientEntityPayload.FromContext(context);

        return Task.FromResult<PanelActionResult>(new OpenModalResult
        {
            ModalType = "RenameClient",
            EntityId = payload.ClientId.ToString()
        });
    }
}
