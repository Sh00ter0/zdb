using Application.Discord.Panels.Core;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Application.Discord.Panels.Middleware;

/// <summary>
/// Logs panel interaction start, completion, duration, and failures.
/// </summary>
public sealed class LoggingMiddleware(ILogger<LoggingMiddleware> logger) : IPanelMiddleware
{
    /// <inheritdoc />
    public async Task<PanelActionResult> InvokeAsync(ConfigPanelContext context, PanelInteraction interaction, Func<ConfigPanelContext, PanelInteraction, Task<PanelActionResult>> next)
    {
        logger.LogInformation("➡️ Panel Interaction Started: [Panel: {Panel} | Action: {Action} | User: {UserId}]",
            interaction.Panel, interaction.Action, context.UserId);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            // The middleware must not change interaction semantics; it only observes execution.
            var result = await next(context, interaction);

            stopwatch.Stop();
            logger.LogInformation("Panel Interaction Success: [Action: {Action} | Time: {ElapsedMs}ms | Result: {ResultType}]",
                interaction.Action, stopwatch.ElapsedMilliseconds, result.GetType().Name);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Panel Interaction Failed: [Action: {Action} | Time: {ElapsedMs}ms]",
                interaction.Action, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
