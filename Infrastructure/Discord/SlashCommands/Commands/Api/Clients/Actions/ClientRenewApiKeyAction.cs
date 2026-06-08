using Application.Repositories;
using Application.Services.API;
using Application.Services.Discord;
using Discord;
using Discord.Interactions;
using Domain.Entities;
using Infrastructure.Exceptions;

namespace Infrastructure.Discord.SlashCommands.Commands.Api.Clients.Actions;

public sealed class ClientRenewApiKeyAction(
    IApiSecurityStore apiSecurityStore,
    IIntegrationClientRepository apiClientRepository,
    IDiscordEmoteService emoteCache,
    ApiClientUiBuilder uiBuilder)
{
    public async Task ShowConfirmationAsync(DiscordInteractionView module, IntegrationClients client)
    {
        var confirmEmote = emoteCache.GetEmote("UI_ICON_CHECK_WHITE");
        var undoEmote = emoteCache.GetEmote("UI_ICON_UNDO");

        var renewComponents = uiBuilder.CreateOverviewContainer(client, cb =>
        {
            cb.WithTextDisplay(
                """
                ### ⚠️ `WARNING`
                Renewing the API Key will **immediately invalidate the current key**. Any external system using the old key will lose access until updated.
                
                **This action is irreversible. Are you sure you want to proceed?**
                """);
            cb.WithActionRow(row =>
            {
                row.AddComponent(new ButtonBuilder().WithCustomId($"client_btn_renew_key_confirm:{client.Id}").WithLabel("Confirm").WithStyle(ButtonStyle.Danger).WithEmote(confirmEmote));
                row.AddComponent(new ButtonBuilder().WithCustomId($"client_btn_cancel:{client.Id}").WithLabel("Return").WithStyle(ButtonStyle.Secondary).WithEmote(undoEmote));
            });
        });

        await ((IComponentInteraction)module.Context.Interaction).UpdateAsync(msg => msg.Components = renewComponents);
    }

    public async Task ConfirmAsync(DiscordInteractionView module, long clientId)
    {
        await module.DeferInteractionAsync(ephemeral: true);

        var undoEmote = emoteCache.GetEmote("UI_ICON_UNDO");

        var newKey = await apiSecurityStore.RenewApiKeyAsync(clientId);
        var client = await apiClientRepository.GetByIdAsync(clientId);
        if (client == null) throw new Exceptions.InteractionException("Client not found.");

        var components = uiBuilder.CreateOverviewContainer(client, cb =>
        {
            cb.WithTextDisplay($"🔒 **NEW API KEY GENERATED:**\n`{newKey}`\n\n*Important: Copy and store this key now. It will not be shown again.*");
            cb.WithActionRow(row => row.AddComponent(new ButtonBuilder().WithCustomId($"client_btn_cancel:{clientId}").WithLabel("Return").WithStyle(ButtonStyle.Secondary).WithEmote(undoEmote)));
        });

        await ((IComponentInteraction)module.Context.Interaction).ModifyOriginalResponseAsync(msg => msg.Components = components);
    }
}
