using Application.Repositories;
using Application.Services.Discord;
using Discord;
using Discord.Interactions;
using Domain.Entities;
using Infrastructure.Exceptions;

namespace Infrastructure.Discord.SlashCommands.Commands.Api.Clients.Actions;

public sealed class ClientStatusAction(
    IIntegrationClientRepository apiClientRepository,
    IDiscordEmoteService emoteCache,
    ApiClientUiBuilder uiBuilder,
    ApiClientPanelRenderer panelRenderer)
{
    public async Task ShowPanelAsync(DiscordInteractionView module, IntegrationClients client)
    {
        var undoEmote = emoteCache.GetEmote("UI_ICON_UNDO");
        var statusComponents = uiBuilder.CreateOverviewContainer(client, cb =>
        {
            cb.WithActionRow(row => row.AddComponent(uiBuilder.GetStatusSelectMenuBuilder($"client_select_status:{client.Id}", client.IsActive)));
            cb.WithActionRow(row => row.AddComponent(new ButtonBuilder().WithCustomId($"client_btn_cancel:{client.Id}").WithLabel("Return").WithStyle(ButtonStyle.Secondary).WithEmote(undoEmote)));
        });

        await ((IComponentInteraction)module.Context.Interaction).UpdateAsync(msg => msg.Components = statusComponents);
    }

    public async Task HandleSelectAsync(DiscordInteractionView module, long clientId,
        string[] selectedValues)
    {
        var newState = bool.Parse(selectedValues[0]);

        var client = await apiClientRepository.GetByIdAsync(clientId);
        if (client == null) throw new UserVisibleException("Client not found.");

        client.IsActive = newState;
        var success = await apiClientRepository.UpdateAsync(client);
        if (!success) throw new UserVisibleException("Failed to update client status.");

        var components = panelRenderer.CreateManagementPanel(client, module.Context);

        await ((IComponentInteraction)module.Context.Interaction).UpdateAsync(msg => msg.Components = components);
        await module.FollowupInteractionAsync($"Client status has been updated to: **{(client.IsActive ? "ACTIVE" : "DISABLED")}**.", ephemeral: true);
    }
}
