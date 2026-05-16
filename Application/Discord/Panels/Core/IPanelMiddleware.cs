namespace Application.Discord.Panels.Core;

/// <summary>
/// Defines a pipeline component that can observe or wrap panel action execution.
/// </summary>
public interface IPanelMiddleware
{
    /// <summary>
    /// Invokes the middleware for the current panel interaction.
    /// </summary>
    /// <param name="context">The current panel request context.</param>
    /// <param name="interaction">The decoded panel interaction.</param>
    /// <param name="next">The next middleware or terminal handler delegate.</param>
    /// <returns>The panel action result produced by the pipeline.</returns>
    Task<PanelActionResult> InvokeAsync(
        ConfigPanelContext context,
        PanelInteraction interaction,
        Func<ConfigPanelContext, PanelInteraction, Task<PanelActionResult>> next);
}
