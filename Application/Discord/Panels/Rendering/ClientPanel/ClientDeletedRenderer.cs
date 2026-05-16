using Application.Discord.Panels.ClientPanel.States;
using Application.Discord.Panels.Core;
using Application.Discord.Panels.LayoutBuilders.ClientPanel;

namespace Application.Discord.Panels.Rendering.ClientPanel;

/// <summary>
/// Renders the final client deletion state for the client management panel.
/// </summary>
public sealed class ClientDeletedRenderer(ClientDeletedLayoutBuilder layoutBuilder) : IPanelViewRenderer
{
    /// <inheritdoc />
    public bool CanRender(IPanelViewState state) => state is ClientDeletedState;

    /// <inheritdoc />
    public Task<RenderedPanel> RenderAsync(IPanelViewState state)
    {
        return Task.FromResult(new RenderedPanel
        {
            Layout = layoutBuilder.Build((ClientDeletedState)state)
        });
    }
}
