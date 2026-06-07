using Application.Repositories;
using Application.Services.Discord;
using Discord;
using Discord.Interactions;
using Domain.Entities;
using Infrastructure.Exceptions;

namespace Infrastructure.Discord.SlashCommands.Commands.Api.Clients.Actions;

public sealed class ClientRemoveAction(
    IIntegrationClientRepository apiClientRepository,
    IDiscordUiService discordUiService,
    IDiscordEmoteService emoteCache,
    ApiClientUiBuilder uiBuilder)
{
    public async Task ShowConfirmationAsync(DiscordInteractionView module, IntegrationClients client)
    {
        var confirmEmote = emoteCache.GetEmote("UI_ICON_CHECK_WHITE");
        var undoEmote = emoteCache.GetEmote("UI_ICON_UNDO");

        var removeComponents = uiBuilder.CreateOverviewContainer(client, cb =>
        {
            cb.WithTextDisplay(
                """
                ### 🛑 `WARNING`
                This will permanently delete this client and ALL associated targets from the database.
                
                **This action is irreversible. Are you sure you want to proceed?**
                """);
            cb.WithActionRow(row =>
            {
                row.AddComponent(new ButtonBuilder().WithCustomId($"client_btn_remove_confirm:{client.Id}").WithLabel("Confirm").WithStyle(ButtonStyle.Danger).WithEmote(confirmEmote));
                row.AddComponent(new ButtonBuilder().WithCustomId($"client_btn_cancel:{client.Id}").WithLabel("Return").WithStyle(ButtonStyle.Secondary).WithEmote(undoEmote));
            });
        });

        await ((IComponentInteraction)module.Context.Interaction).UpdateAsync(msg => msg.Components = removeComponents);
    }

    public async Task ConfirmAsync(DiscordInteractionView module, long clientId)
    {
        var success = await apiClientRepository.DeleteAsync(clientId);
        if (!success) throw new UserVisibleException("Failed to remove client. It may have already been deleted.");

        var deletedComponents = discordUiService.CreateStandardContainer(
            header: "Client Removed",
            body: "Api client has been permanently removed.",
            accentColor: Color.Red);

        await ((IComponentInteraction)module.Context.Interaction).UpdateAsync(msg => msg.Components = deletedComponents);
    }
}
