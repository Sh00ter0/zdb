namespace Application.Discord.Panels.Core;

/// <summary>
/// Handles a single final panel action.
/// </summary>
public interface IPanelActionHandler
{
    /// <summary>
    /// Gets the action identifier handled by this instance.
    /// </summary>
    string Action { get; }

    /// <summary>
    /// Executes the action and returns the next panel intent.
    /// </summary>
    /// <param name="context">The current panel request context.</param>
    /// <param name="interaction">The decoded panel interaction.</param>
    /// <returns>The result describing the next response step.</returns>
    Task<PanelActionResult> ExecuteAsync(ConfigPanelContext context, PanelInteraction interaction);
}
