using Application.Discord.Panels.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Discord.Panels.Rendering;

/// <summary>
/// Selects the registered view renderer that can render the supplied panel state.
/// </summary>
public sealed class DiscordPanelRenderer(IEnumerable<IPanelViewRenderer> viewRenderers) : IPanelRenderer
{
    /// <inheritdoc />
    public Task<RenderedPanel> RenderAsync(IPanelViewState state)
    {
        var renderer = viewRenderers.FirstOrDefault(x => x.CanRender(state))
            ?? throw new InvalidOperationException($"No renderer found for state: {state.GetType().Name}");

        return renderer.RenderAsync(state);
    }
}
