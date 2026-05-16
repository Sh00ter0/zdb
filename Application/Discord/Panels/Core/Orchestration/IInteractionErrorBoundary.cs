using System;

namespace Application.Discord.Panels.Core.Orchestration;

/// <summary>
/// Handles exceptions thrown while dispatching panel interactions.
/// </summary>
public interface IInteractionErrorBoundary
{
    /// <summary>
    /// Converts an exception into a panel action result that can be safely rendered.
    /// </summary>
    /// <param name="exception">The exception thrown during interaction handling.</param>
    /// <param name="context">The current panel request context.</param>
    /// <param name="interaction">The decoded interaction being processed.</param>
    /// <returns>A recoverable panel action result.</returns>
    PanelActionResult HandleException(Exception exception, ConfigPanelContext context, PanelInteraction interaction);
}
