using Application.Discord.Panels.Shared.States;
using Microsoft.Extensions.Logging;
using System;

namespace Application.Discord.Panels.Core.Orchestration;

/// <summary>
/// Converts unexpected interaction exceptions into a safe error panel state.
/// </summary>
public sealed class DefaultErrorBoundary(ILogger<DefaultErrorBoundary> logger) : IInteractionErrorBoundary
{
    /// <inheritdoc />
    public PanelActionResult HandleException(Exception exception, ConfigPanelContext context, PanelInteraction interaction)
    {
        logger.LogError(exception, "Recovered from fatal interaction error in panel {Panel}", interaction.Panel);

        return new UpdatePanelResult
        {
            State = new PanelErrorState
            {
                ErrorMessage = exception.Message,
                ReferenceId = Guid.NewGuid().ToString("N")[..8]
            }
        };
    }
}
