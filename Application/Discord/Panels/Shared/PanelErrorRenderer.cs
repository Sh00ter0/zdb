using Application.Discord.Panels.Core;
using Application.Discord.Panels.LayoutBuilders;
using Application.Discord.Panels.Shared.States;

namespace Application.Discord.Panels.Rendering.Shared;

/// <summary>
/// Renders shared panel error states into Discord panel output.
/// </summary>
public sealed class PanelErrorRenderer(PanelErrorLayoutBuilder layoutBuilder) : IPanelViewRenderer
{
    /// <inheritdoc />
    public bool CanRender(IPanelViewState state) => state is PanelErrorState;

    /// <inheritdoc />
    public Task<RenderedPanel> RenderAsync(IPanelViewState state)
    {
        return Task.FromResult(new RenderedPanel
        {
            Layout = layoutBuilder.Build((PanelErrorState)state)
        });
    }
}
