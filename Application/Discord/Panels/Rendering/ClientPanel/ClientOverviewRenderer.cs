using Application.Discord.Panels.ClientPanel.States;
using Application.Discord.Panels.Core;
using Application.Discord.Panels.LayoutBuilders;

namespace Application.Discord.Panels.Rendering.ClientPanel;

/// <summary>
/// Renders the client overview state for the client management panel.
/// </summary>
public sealed class ClientOverviewRenderer(ClientOverviewLayoutBuilder layoutBuilder) : IPanelViewRenderer
{
    /// <inheritdoc />
    public bool CanRender(IPanelViewState state) => state is ClientOverviewState;

    /// <inheritdoc />
    public Task<RenderedPanel> RenderAsync(IPanelViewState state)
    {
        return Task.FromResult(new RenderedPanel
        {
            Layout = layoutBuilder.Build((ClientOverviewState)state)
        });
    }
}
