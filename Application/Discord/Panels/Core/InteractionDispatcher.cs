using Application.Discord.Panels.Core.Orchestration;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace Application.Discord.Panels.Core;

/// <summary>
/// Decodes Discord interaction identifiers and coordinates panel action execution.
/// </summary>
public sealed class InteractionDispatcher(
    IInteractionCodec codec,
    InteractionPipeline pipeline,
    IInteractionErrorBoundary errorBoundary,
    ModalCoordinator modals,
    InteractionResponseHandler responseHandler,
    ILogger<InteractionDispatcher> logger)
{
    /// <summary>
    /// Dispatches a Discord interaction to the declarative panel pipeline.
    /// </summary>
    /// <param name="context">The current Discord interaction context.</param>
    /// <param name="services">The scoped service provider for the interaction.</param>
    /// <param name="customId">The Discord custom id to decode.</param>
    /// <param name="selectedValues">Optional select-menu or modal values supplied by Discord.</param>
    public async Task DispatchAsync(SocketInteractionContext context, IServiceProvider services, string customId, string[]? selectedValues = null)
    {
        PanelInteraction? interaction = null;
        var userId = context.User.Id;

        try
        {
            interaction = codec.Decode(customId);

            var panelContext = new ConfigPanelContext
            {
                Context = context,
                Services = services,
                UserId = userId,
                EntityId = interaction.EntityId,
                RawInteractionData = selectedValues
            };

            var result = await pipeline.ExecuteAsync(panelContext, interaction);

            switch (result)
            {
                case OpenModalResult modalResult:
                    var modal = modals.BuildModal(modalResult);
                    await context.Interaction.RespondWithModalAsync(modal);
                    break;
                case ClosePanelResult closeResult:
                    await responseHandler.HandleCloseAsync(context, closeResult);
                    break;
                case UpdatePanelResult updateResult:
                    await responseHandler.HandleUpdateAsync(context, updateResult);
                    break;
            }
        }
        catch (Exception ex)
        {
            if (interaction != null)
            {
                var errorContext = new ConfigPanelContext
                {
                    Context = context,
                    Services = services,
                    UserId = userId,
                    EntityId = interaction.EntityId,
                    RawInteractionData = selectedValues
                };

                var errorResult = errorBoundary.HandleException(ex, errorContext, interaction);
                if (errorResult is UpdatePanelResult updateResult)
                {
                    await responseHandler.HandleUpdateAsync(context, updateResult);
                    return;
                }
            }

            logger.LogError(ex, "Critical failure dispatching customId: {CustomId}", customId);
            try
            {
                if (!context.Interaction.HasResponded)
                    await context.Interaction.RespondAsync($"Critical error: {ex.Message}", ephemeral: true);
                else
                    await context.Interaction.FollowupAsync($"Critical error: {ex.Message}", ephemeral: true);
            }
            catch
            {
            }
        }
    }
}
