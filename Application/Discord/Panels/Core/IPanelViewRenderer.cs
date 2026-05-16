using System.Threading.Tasks;

namespace Application.Discord.Panels.Core;

/// <summary>
/// Renders one or more specific panel view state types.
/// </summary>
public interface IPanelViewRenderer
{
    /// <summary>
    /// Determines whether this renderer can render the supplied state.
    /// </summary>
    /// <param name="state">The state to test.</param>
    /// <returns><see langword="true"/> when the renderer supports the state.</returns>
    bool CanRender(IPanelViewState state);

    /// <summary>
    /// Renders the supplied state into a panel response model.
    /// </summary>
    /// <param name="state">The state to render.</param>
    /// <returns>The rendered panel output.</returns>
    Task<RenderedPanel> RenderAsync(IPanelViewState state);
}
