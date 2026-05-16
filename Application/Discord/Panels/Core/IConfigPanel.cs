using System.Threading.Tasks;

namespace Application.Discord.Panels.Core;

/// <summary>
/// Defines a logical Discord control panel that can build state and execute actions.
/// </summary>
public interface IConfigPanel
{
    /// <summary>
    /// Gets the stable panel identifier encoded into panel interactions.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Builds the default view state for this panel.
    /// </summary>
    /// <param name="context">The current panel request context.</param>
    /// <returns>The default view state for the panel.</returns>
    Task<IPanelViewState> BuildStateAsync(ConfigPanelContext context);

    /// <summary>
    /// Executes the final decoded panel action.
    /// </summary>
    /// <param name="context">The current panel request context.</param>
    /// <param name="interaction">The decoded panel interaction.</param>
    /// <returns>The action result that describes how the response should proceed.</returns>
    Task<PanelActionResult> ExecuteActionAsync(ConfigPanelContext context, PanelInteraction interaction);
}
