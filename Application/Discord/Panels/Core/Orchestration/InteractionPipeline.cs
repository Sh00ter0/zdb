namespace Application.Discord.Panels.Core.Orchestration;

/// <summary>
/// Executes panel middleware and dispatches the interaction to the target panel.
/// </summary>
public sealed class InteractionPipeline(
    IEnumerable<IPanelMiddleware> middlewares,
    IPanelRegistry registry)
{
    /// <summary>
    /// Runs the configured middleware chain and invokes the final panel handler.
    /// </summary>
    /// <param name="context">The current panel request context.</param>
    /// <param name="interaction">The decoded panel interaction.</param>
    /// <returns>The result produced by the panel action pipeline.</returns>
    public async Task<PanelActionResult> ExecuteAsync(ConfigPanelContext context, PanelInteraction interaction)
    {
        Func<ConfigPanelContext, PanelInteraction, Task<PanelActionResult>> pipeline = async (ctx, inter) =>
        {
            var panel = registry.Get(inter.Panel);
            return await panel.ExecuteActionAsync(ctx, inter);
        };

        foreach (var middleware in middlewares.Reverse())
        {
            var next = pipeline;
            pipeline = (ctx, inter) => middleware.InvokeAsync(ctx, inter, next);
        }

        return await pipeline(context, interaction);
    }
}
