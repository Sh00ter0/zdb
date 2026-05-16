using Application.Discord.Panels.ClientPanel.States;
using Application.Discord.Panels.Core;
using Application.Discord.Panels.LayoutBuilders;

namespace Application.Discord.Panels.Rendering.ClientPanel;

/// <summary>
/// Renders the client status selection state for the client management panel.
/// </summary>
public sealed class ClientStatusRenderer(ClientStatusLayoutBuilder layoutBuilder) : IPanelViewRenderer
{
    /// <inheritdoc />
    public bool CanRender(IPanelViewState state) => state is ClientStatusState;

    /// <inheritdoc />
    public Task<RenderedPanel> RenderAsync(IPanelViewState state)
    {
        return Task.FromResult(new RenderedPanel
        {
            Layout = layoutBuilder.Build((ClientStatusState)state)
        });
    }
}
