using Application.Repositories;
using Application.Services.Discord;
using Discord;
using Discord.Interactions;
using Domain.Entities;
using Infrastructure.Exceptions;

namespace Infrastructure.Discord.SlashCommands.Commands.Api.KnownTargets.Actions;

public sealed class TargetCrosspostAction(
    IIntegrationClientRepository apiClientRepository,
    IKnownDeliveryTargetRepository targetRepository,
    IDiscordEmoteService emoteCache,
    KnownTargetUiBuilder uiBuilder,
    KnownTargetPanelRenderer panelRenderer)
{
    public async Task ShowPanelAsync(DiscordInteractionView module, IntegrationClients client,
        KnownDeliveryTargets target)
    {
        var undoEmote = emoteCache.GetEmote("UI_ICON_UNDO");
        var components = uiBuilder.CreateOverviewContainer(client.Name, target, cb =>
        {
            cb.WithActionRow(row => row.AddComponent(uiBuilder.GetCrosspostSelectMenuBuilder($"target_select_crosspost:{client.Id}:{target.TargetId}", target.AutoCrosspost)));
            cb.WithActionRow(row => row.AddComponent(new ButtonBuilder().WithCustomId($"target_btn_cancel:{client.Id}:{target.TargetId}").WithLabel("Return").WithStyle(ButtonStyle.Secondary).WithEmote(undoEmote)));
        });

        await ((IComponentInteraction)module.Context.Interaction).UpdateAsync(msg => msg.Components = components);
    }

    public async Task HandleSelectAsync(DiscordInteractionView module, long clientId,
        ulong targetDiscordId, string[] selectedValues)
    {
        var newState = bool.Parse(selectedValues[0]);

        var targetData = await targetRepository.GetByDiscordIdAsync(clientId, targetDiscordId);
        if (targetData == null) throw new UserVisibleException("Target not found.");

        targetData.AutoCrosspost = newState;
        var success = await targetRepository.UpdateAsync(targetData);
        if (!success) throw new UserVisibleException("Failed to locate the target in the database.");

        var client = await apiClientRepository.GetByIdAsync(clientId);
        if (client == null) throw new UserVisibleException("Client not found.");

        var components = panelRenderer.CreateManagementPanel(client, targetData, module.Context);

        await ((IComponentInteraction)module.Context.Interaction).UpdateAsync(msg => msg.Components = components);
        await module.FollowupInteractionAsync($"Auto-Publish mode has been updated to **{newState}**.", ephemeral: true);
    }
}
