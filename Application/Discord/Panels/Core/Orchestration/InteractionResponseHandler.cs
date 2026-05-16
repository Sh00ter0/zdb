using Application.Discord.Panels.Rendering;
using Discord;
using Discord.Interactions;

namespace Application.Discord.Panels.Core.Orchestration;

/// <summary>
/// Applies panel action results to Discord interactions and message updates.
/// </summary>
public sealed class InteractionResponseHandler(
    IPanelRenderer renderer,
    DiscordLayoutMapper layoutMapper)
{
    /// <summary>
    /// Clears the current component message and optionally sends a toast follow-up.
    /// </summary>
    /// <param name="context">The current Discord interaction context.</param>
    /// <param name="result">The close-panel result to apply.</param>
    public async Task HandleCloseAsync(SocketInteractionContext context, ClosePanelResult result)
    {
        if (context.Interaction is IComponentInteraction comp)
        {
            await comp.UpdateAsync(msg =>
            {
                msg.Components = new ComponentBuilder().Build();
                msg.Content = "❌ *Panel closed.*";
            });
        }
        if (!string.IsNullOrEmpty(result.ToastMessage))
            await context.Interaction.FollowupAsync(result.ToastMessage, ephemeral: true);
    }

    /// <summary>
    /// Renders and applies an updated panel state to the current interaction message.
    /// </summary>
    /// <param name="context">The current Discord interaction context.</param>
    /// <param name="result">The update-panel result to apply.</param>
    public async Task HandleUpdateAsync(SocketInteractionContext context, UpdatePanelResult result)
    {
        var panel = await renderer.RenderAsync(result.State);
        var hasEmbeds = panel.Embeds != null && panel.Embeds.Length > 0;
        var finalComponents = panel.Layout != null ? layoutMapper.Map(panel.Layout) : panel.Components;

        void ModifyMessage(MessageProperties msg)
        {
            msg.Content = panel.Content;
            msg.Components = finalComponents;

            if (hasEmbeds)
            {
                msg.Embeds = panel.Embeds;
            }
            else
            {
                msg.Flags = MessageFlags.ComponentsV2;
            }
        }

        if (context.Interaction is IComponentInteraction componentInteraction)
            await componentInteraction.UpdateAsync(ModifyMessage);
        else if (context.Interaction is IModalInteraction modalInteraction)
            await modalInteraction.UpdateAsync(ModifyMessage);

        if (!string.IsNullOrEmpty(result.ToastMessage))
            await context.Interaction.FollowupAsync(result.ToastMessage, ephemeral: true);
    }
}
