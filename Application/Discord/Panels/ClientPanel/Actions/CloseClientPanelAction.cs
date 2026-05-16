using Application.Discord.Panels.Core;

namespace Application.Discord.Panels.ClientPanel.Actions;

/// <summary>
/// Closes the current client panel message.
/// </summary>
public sealed class CloseClientPanelAction : IPanelActionHandler
{
    /// <inheritdoc />
    public string Action => ClientPanelActionIds.ClosePanel;

    /// <inheritdoc />
    public Task<PanelActionResult> ExecuteAsync(ConfigPanelContext context, PanelInteraction interaction)
    {
        return Task.FromResult<PanelActionResult>(new ClosePanelResult());
    }
}
