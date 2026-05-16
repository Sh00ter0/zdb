using System.Threading.Tasks;

namespace Application.Discord.Panels.Core;

/// <summary>
/// Renders panel view states into transport-ready panel output.
/// </summary>
public interface IPanelRenderer
{
    /// <summary>
    /// Renders a view state using the matching registered view renderer.
    /// </summary>
    /// <param name="state">The state to render.</param>
    /// <returns>The rendered panel output.</returns>
    Task<RenderedPanel> RenderAsync(IPanelViewState state);
}
