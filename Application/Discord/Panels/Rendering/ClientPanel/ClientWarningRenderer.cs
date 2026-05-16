using Application.Discord.Panels.ClientPanel.States;
using Application.Discord.Panels.Core;
using Application.Discord.Panels.LayoutBuilders.ClientPanel;

namespace Application.Discord.Panels.Rendering.ClientPanel;

/// <summary>
/// Renders warning and confirmation states for sensitive client panel actions.
/// </summary>
public sealed class ClientWarningRenderer(ClientWarningLayoutBuilder layoutBuilder) : IPanelViewRenderer
{
    /// <inheritdoc />
    public bool CanRender(IPanelViewState state) => state is ClientWarningState;

    /// <inheritdoc />
    public Task<RenderedPanel> RenderAsync(IPanelViewState state)
    {
        return Task.FromResult(new RenderedPanel
        {
            Layout = layoutBuilder.Build((ClientWarningState)state)
        });
    }
}
