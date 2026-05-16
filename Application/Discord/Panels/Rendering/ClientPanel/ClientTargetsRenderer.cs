using Application.Discord.Panels.ClientPanel.States;
using Application.Discord.Panels.Core;
using Application.Discord.Panels.LayoutBuilders.ClientPanel;

namespace Application.Discord.Panels.Rendering.ClientPanel;

/// <summary>
/// Renders the client delivery targets state for the client management panel.
/// </summary>
public sealed class ClientTargetsRenderer(ClientTargetsLayoutBuilder layoutBuilder) : IPanelViewRenderer
{
    /// <inheritdoc />
    public bool CanRender(IPanelViewState state) => state is ClientTargetsState;

    /// <inheritdoc />
    public Task<RenderedPanel> RenderAsync(IPanelViewState state)
    {
        return Task.FromResult(new RenderedPanel
        {
            Layout = layoutBuilder.Build((ClientTargetsState)state)
        });
    }
}
