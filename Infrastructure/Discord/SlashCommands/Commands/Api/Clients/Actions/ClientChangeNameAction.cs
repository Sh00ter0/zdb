using Application.Repositories;
using Application.Services.Discord;
using Discord;
using Discord.Interactions;
using Infrastructure.Exceptions;
using Infrastructure.Models.Modals;

namespace Infrastructure.Discord.SlashCommands.Commands.Api.Clients.Actions;

public sealed class ClientChangeNameAction(
    IDiscordUiService discordUiService,
    IIntegrationClientRepository apiClientRepository,
    ApiClientPanelRenderer panelRenderer)
{
    public Task ShowModalAsync(DiscordInteractionView module, long clientId)
    {
        var renameModal = discordUiService.CreateConfirmationModal(
            customId: $"client_modal_rename:{clientId}",
            title: "Rename API Client",
            inputLabel: "New Display Name",
            placeholder: "Enter new unique name...",
            maxLength: 50);

        return module.RespondWithModalInteractionAsync(renameModal);
    }

    public async Task HandleModalAsync(DiscordInteractionView module, long clientId,
        ClientActionModal modal)
    {
        var newName = modal.ConfirmText.Trim();
        try
        {
            var client = await apiClientRepository.GetByIdAsync(clientId);
            if (client == null) throw new Exceptions.InteractionException("Client not found.");

            client.Name = newName;
            var success = await apiClientRepository.UpdateAsync(client);
            if (!success) throw new Exceptions.InteractionException("Failed to update the client.");

            var components = panelRenderer.CreateManagementPanel(client, module.Context);

            await ((IModalInteraction)module.Context.Interaction).UpdateAsync(msg => msg.Components = components);
            await module.FollowupInteractionAsync($"Client successfully renamed to `{newName}`.", ephemeral: true);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            throw new Exceptions.InteractionException($"Failed to rename client. The name \"{newName}\" is already used.", logCopy: true);
        }
    }
}
