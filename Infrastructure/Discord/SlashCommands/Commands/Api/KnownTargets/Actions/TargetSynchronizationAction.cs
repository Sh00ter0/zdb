using Application.Repositories;
using Application.Services.Discord;
using Discord;
using Discord.Interactions;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Exceptions;

namespace Infrastructure.Discord.SlashCommands.Commands.Api.KnownTargets.Actions;

public sealed class TargetSynchronizationAction(
    IIntegrationClientRepository apiClientRepository,
    IKnownDeliveryTargetRepository targetRepository,
    IDiscordTargetSyncService syncService,
    IDiscordEmoteService emoteCache,
    KnownTargetUiBuilder uiBuilder,
    KnownTargetPanelRenderer panelRenderer)
{
    public async Task ShowConfirmationAsync(DiscordInteractionView module,
        IntegrationClients client, KnownDeliveryTargets target)
    {
        var confirmEmote = emoteCache.GetEmote("UI_ICON_CHECK_WHITE");
        var undoEmote = emoteCache.GetEmote("UI_ICON_UNDO");

        var components = uiBuilder.CreateOverviewContainer(client.Name, target, cb =>
        {
            cb.WithTextDisplay(
                """
                ### ⚠️ `WARNING`
                This action will force a resynchronization with Discord's current data. If the channel type is no longer supported, the target will be automatically removed.
                
                **This action is irreversible. Are you sure you want to proceed?**
                """);
            cb.WithActionRow(row =>
            {
                row.AddComponent(new ButtonBuilder().WithCustomId($"target_btn_sync_confirm:{client.Id}:{target.TargetId}").WithLabel("Confirm").WithStyle(ButtonStyle.Danger).WithEmote(confirmEmote));
                row.AddComponent(new ButtonBuilder().WithCustomId($"target_btn_cancel:{client.Id}:{target.TargetId}").WithLabel("Return").WithStyle(ButtonStyle.Secondary).WithEmote(undoEmote));
            });
        });

        await ((IComponentInteraction)module.Context.Interaction).UpdateAsync(msg => msg.Components = components);
    }

    public async Task ConfirmAsync(DiscordInteractionView module, long clientId,
        ulong targetDiscordId)
    {
        await module.DeferInteractionAsync(ephemeral: true);

        var target = await targetRepository.GetByDiscordIdAsync(clientId, targetDiscordId);
        if (target == null) throw new UserVisibleException("Target not found.");

        IChannel? resolvedChannel = null;
        IUser? resolvedUser = null;

        if (target.ChannelType == TextChannelType.DirectMessage)
            resolvedUser = (module.Context.Client.GetUser(targetDiscordId) as IUser) ?? await module.Context.Client.Rest.GetUserAsync(targetDiscordId);
        else
            resolvedChannel = (module.Context.Client.GetChannel(targetDiscordId) as IChannel) ?? await module.Context.Client.Rest.GetChannelAsync(targetDiscordId);

        var result = await syncService.VerifyAndUpdateTargetAsync(target, resolvedChannel, resolvedUser);
        if (result is null) throw new UserVisibleException("Failed to synchronize target. It violates the allowed channel types and was automatically removed.");

        var client = await apiClientRepository.GetByIdAsync(clientId);
        if (client == null) throw new UserVisibleException("Client not found.");

        var components = panelRenderer.CreateManagementPanel(client, result, module.Context);

        await ((IComponentInteraction)module.Context.Interaction).ModifyOriginalResponseAsync(msg => msg.Components = components);
        await module.FollowupInteractionAsync("Synchronization complete.", ephemeral: true);
    }
}
