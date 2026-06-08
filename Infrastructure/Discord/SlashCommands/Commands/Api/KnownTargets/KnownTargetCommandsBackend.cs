using Application.Repositories;
using Application.Services.Discord;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Discord.SlashCommands.Commands.Api.KnownTargets.Actions;
using Infrastructure.Exceptions;
using Infrastructure.Extensions;
using Infrastructure.Models.Modals;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Discord.SlashCommands.Commands.Api.KnownTargets;

public sealed class KnownTargetCommandsBackend(
    ILogger<KnownTargetCommandsBackend> logger,
    IIntegrationClientRepository apiClientRepository,
    IKnownDeliveryTargetRepository targetRepository,
    IDiscordUiService discordUiService,
    KnownTargetPanelRenderer panelRenderer,
    TargetChangeNameAction changeNameAction,
    TargetCrosspostAction crosspostAction,
    TargetSynchronizationAction synchronizationAction,
    TargetRemoveAction removeAction,
    TargetCancelAction cancelAction)
{
    public async Task AddTargetAsync(DiscordInteractionView module, string clientName,
        string friendlyName, IChannel? channel = null, IUser? user = null, bool autoCrosspost = false)
    {
        await module.DeferInteractionAsync(ephemeral: true);

        if (channel == null && user == null) throw new Exceptions.InteractionException("You must select either a Channel or a User to authorize.");
        if (channel != null && user != null) throw new Exceptions.InteractionException("Please select ONLY ONE option (Channel OR User).");

        ulong targetId = 0;
        var type = TextChannelType.Unknown;
        ulong? guildId = null;

        if (user != null)
        {
            targetId = user.Id;
            type = TextChannelType.DirectMessage;
        }
        else if (channel != null)
        {
            targetId = channel.Id;
            if (channel is IGuildChannel guildChannel) guildId = guildChannel.GuildId;

            if (channel is INewsChannel) type = TextChannelType.GuildAnnouncementChannel;
            if (channel is IForumChannel) throw new Exceptions.InteractionException("Forum channels cannot be directly authorized. Please select a thread within the forum to authorize.");
            else if (channel is SocketThreadChannel thread)
            {
                if (thread.ParentChannel is IForumChannel) type = TextChannelType.GuildForumThreadChannel;
                else if (thread.Type == ThreadType.PrivateThread) type = TextChannelType.GuildPrivateThreadChannel;
                else type = TextChannelType.GuildPublicThreadChannel;
            }
            else if (channel is IThreadChannel) type = TextChannelType.GuildPublicThreadChannel;
            else if (channel is ITextChannel && channel is not IVoiceChannel && channel is not IStageChannel) type = TextChannelType.GuildTextChannel;
            else if (channel is IStageChannel) type = TextChannelType.GuildStageVoiceTextChannel;
            else if (channel is IVoiceChannel) type = TextChannelType.GuildVoiceTextChannel;
        }

        var client = await apiClientRepository.GetByNameAsync(clientName);
        if (client == null || !client.IsActive) throw new Exceptions.InteractionException($"Failed to add target. Active API Client `{clientName}` was not found.");

        var newTarget = new KnownDeliveryTargets
        {
            IntegrationClientId = client.Id,
            TargetId = targetId,
            ChannelType = type,
            Name = friendlyName,
            AssociatedGuildId = guildId,
            CreatedById = module.Context.Admin!.Id,
            AutoCrosspost = autoCrosspost,
            CreatedAtUtc = DateTime.UtcNow
        };

        try
        {
            var success = await targetRepository.AddAsync(newTarget);
            if (!success) throw new Exceptions.InteractionException("An unexpected database error occurred while adding the target.");
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            throw new Exceptions.InteractionException($"Failed to add target. The target has already been authorized or the display name is not unique for `{clientName}`.");
        }

        if (newTarget.ChannelType is TextChannelType.DirectMessage)
        {
            var userNotification = discordUiService.CreateStandardContainer(
                header: "Authorization to receive notifications granted",
                body: $"""
                Hello {user!.Mention},

                You have been authorized to become a notification beneficiary for the API client `{client.Name}`.
                This means that this client can send you direct messages through the bot, and these messages will be delivered to you as notifications.

                If it's a mistake or you wish to revoke this access, please contact {module.Context.User.Mention} immediately.

                -# ⚠️ Please, keep in mind. Information transmitted through this communication channel may be **confidential** or **sensitive** in nature. Please **handle it with care** and do not share it with unauthorized parties.
                """,
                accentColor: AppColors.Warning);

            try
            {
                await user!.SendMessageAsync(components: userNotification);
            }
            catch
            {
            }
        }

        var bodyText = $"""
                        **Client name:** {clientName}
                        **Name:** `{friendlyName}`
                        **Discord target ID:** `{targetId}`
                        **Type:** `{type.GetDiscordLabel()}`
                        **Auto-Crosspost:** `{autoCrosspost}`
                        """;

        var components = discordUiService.CreateStandardContainer(header: "Target authorized", body: bodyText);
        await module.FollowupInteractionAsync(components: components, flags: MessageFlags.ComponentsV2, ephemeral: true);
        logger.LogInformation("Admin {AdminId} authorized target {TargetId} for client {ClientId}", module.Context.User.Id, targetId, client.Id);
    }

    public async Task ManageTargetAsync(DiscordInteractionView module, string clientName,
        string rawTargetId)
    {
        var client = await apiClientRepository.GetByNameAsync(clientName);
        if (client is null) throw new Exceptions.InteractionException($"API Client `{clientName}` not found.");

        if (!ulong.TryParse(rawTargetId, out var targetDiscordId))
        {
            throw new Exceptions.InteractionException("Invalid target format. Please select a valid target from the autocomplete list.");
        }

        var target = await targetRepository.GetByDiscordIdAsync(client.Id, targetDiscordId);
        if (target == null) throw new Exceptions.InteractionException("Target not found.");

        var components = panelRenderer.CreateManagementPanel(client, target, module.Context);

        await module.RespondInteractionAsync(components: components, ephemeral: true, flags: MessageFlags.ComponentsV2);
    }

    public async Task HandleTargetActionSelectAsync(DiscordInteractionView module, long clientId,
        ulong targetDiscordId, string[] selectedValues)
    {
        var action = Enum.Parse<AllowedTargetModifyingAction>(selectedValues[0]);
        var client = await apiClientRepository.GetByIdAsync(clientId);
        var target = await targetRepository.GetByDiscordIdAsync(clientId, targetDiscordId);

        if (client == null || target == null) throw new Exceptions.InteractionException("Target or client not found.");

        switch (action)
        {
            case AllowedTargetModifyingAction.ChangeFriendlyName:
                await changeNameAction.ShowModalAsync(module, clientId, targetDiscordId);
                break;

            case AllowedTargetModifyingAction.ChangeCrosspostMode:
                await crosspostAction.ShowPanelAsync(module, client, target);
                break;

            case AllowedTargetModifyingAction.SynchronizeTargetData:
                await synchronizationAction.ShowConfirmationAsync(module, client, target);
                break;

            case AllowedTargetModifyingAction.Remove:
                await removeAction.ShowConfirmationAsync(module, client, target);
                break;
        }
    }

    public Task HandleCancelManageAsync(DiscordInteractionView module, long clientId,
        ulong targetDiscordId)
    {
        return cancelAction.ExecuteAsync(module, clientId, targetDiscordId);
    }

    public Task HandleRenameModalAsync(DiscordInteractionView module, long clientId,
        ulong targetDiscordId, ClientActionModal modal)
    {
        return changeNameAction.HandleModalAsync(module, clientId, targetDiscordId, modal);
    }

    public Task HandleCrosspostSelectAsync(DiscordInteractionView module, long clientId,
        ulong targetDiscordId, string[] selectedValues)
    {
        return crosspostAction.HandleSelectAsync(module, clientId, targetDiscordId, selectedValues);
    }

    public Task HandleSyncConfirmAsync(DiscordInteractionView module, long clientId,
        ulong targetDiscordId)
    {
        return synchronizationAction.ConfirmAsync(module, clientId, targetDiscordId);
    }

    public Task HandleRemoveConfirmAsync(DiscordInteractionView module, long clientId,
        ulong targetDiscordId)
    {
        return removeAction.ConfirmAsync(module, clientId, targetDiscordId);
    }
}
