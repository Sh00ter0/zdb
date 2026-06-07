using Application.Repositories;
using Application.Services.Discord;
using Discord;
using Discord.Interactions;
using Infrastructure.Exceptions;
using Infrastructure.Models.Modals;

namespace Infrastructure.Discord.SlashCommands.Commands.Api.KnownTargets.Actions;

public sealed class TargetChangeNameAction(
    IDiscordUiService discordUiService,
    IKnownDeliveryTargetRepository targetRepository,
    KnownTargetPanelRenderer panelRenderer)
{
    public Task ShowModalAsync(DiscordInteractionView module, long clientId,
        ulong targetDiscordId)
    {
        var renameModal = discordUiService.CreateConfirmationModal(
            customId: $"target_modal_rename:{clientId}:{targetDiscordId}",
            title: "Rename Target",
            inputLabel: "New Display Name",
            placeholder: "Enter new unique name...",
            maxLength: 50);

        return module.RespondWithModalInteractionAsync(renameModal);
    }

    public async Task HandleModalAsync(DiscordInteractionView module, long clientId,
        ulong targetDiscordId, ClientActionModal modal)
    {
        var newName = modal.ConfirmText.Trim();
        try
        {
            var targetData = await targetRepository.GetByDiscordIdAsync(clientId, targetDiscordId);
            if (targetData == null) throw new UserVisibleException("Target not found.");

            targetData.Name = newName;
            var success = await targetRepository.UpdateAsync(targetData);
            if (!success) throw new UserVisibleException("Failed to find the target, or the provided name is not unique.");

            var components = await panelRenderer.CreateManagementPanelAsync(clientId, targetDiscordId, module.Context);

            await ((IModalInteraction)module.Context.Interaction).UpdateAsync(msg => msg.Components = components);
            await module.FollowupInteractionAsync($"Target successfully renamed to `{newName}`.", ephemeral: true);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            throw new UserVisibleException($"Failed to rename target. The name `{newName}` is already used by another target in this client.");
        }
    }
}
