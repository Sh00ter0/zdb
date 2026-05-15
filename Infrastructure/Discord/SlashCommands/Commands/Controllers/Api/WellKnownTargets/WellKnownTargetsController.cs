using Application.Repositories;
using Application.Services.API;
using Application.Services.Discord;
using Application.Services.Pagination;
using Discord;
using Discord.WebSocket;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Exceptions;
using Infrastructure.Extensions;
using Infrastructure.Models.Modals;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Discord.SlashCommands.Commands.Controllers.Api.WellKnownTargets;

public class WellKnownTargetsController(
        ILogger<WellKnownTargetsController> logger,
        IIntegrationClientRepository apiClientRepository,
        IKnownDeliveryTargetRepository targetRepository,
        IDiscordUiService discordUiService,
        IDiscordTargetSyncService syncService,
        IDiscordEmoteService emoteCache)
    {
        public async Task AddTargetAsync(AppInteractionContext context, string clientName, string friendlyName, IChannel? channel, IUser? user, bool autoCrosspost)
        {
            await context.Interaction.DeferAsync(ephemeral: true);
            if (channel == null && user == null) throw new UserVisibleException("You must select either a Channel or a User to authorize.");
            if (channel != null && user != null) throw new UserVisibleException("Please select ONLY ONE option (Channel OR User).");

            ulong targetId = user?.Id ?? channel!.Id;
            TextChannelType type = user != null ? TextChannelType.DirectMessage : channel switch
            {
                INewsChannel => TextChannelType.GuildAnnouncementChannel,
                IForumChannel => throw new UserVisibleException("Forum channels cannot be directly authorized. Select a thread."),
                SocketThreadChannel thread => thread.ParentChannel is IForumChannel ? TextChannelType.GuildForumThreadChannel : (thread.Type == ThreadType.PrivateThread ? TextChannelType.GuildPrivateThreadChannel : TextChannelType.GuildPublicThreadChannel),
                IThreadChannel => TextChannelType.GuildPublicThreadChannel,
                ITextChannel and not IVoiceChannel and not IStageChannel => TextChannelType.GuildTextChannel,
                IStageChannel => TextChannelType.GuildStageVoiceTextChannel,
                IVoiceChannel => TextChannelType.GuildVoiceTextChannel,
                _ => TextChannelType.Unknown
            };

            var client = await apiClientRepository.GetByNameAsync(clientName);
            if (client == null || !client.IsActive) throw new UserVisibleException($"Failed to add target. Active API Client `{clientName}` was not found.");

            var newTarget = new KnownDeliveryTargets
            {
                IntegrationClientId = client.Id,
                TargetId = targetId,
                ChannelType = type,
                Name = friendlyName,
                AssociatedGuildId = (channel as IGuildChannel)?.GuildId,
                CreatedById = context.Admin!.Id,
                AutoCrosspost = autoCrosspost,
                CreatedAtUtc = DateTime.UtcNow
            };

            try { await targetRepository.AddAsync(newTarget); }
            catch (DbUpdateException) { throw new UserVisibleException($"Failed to add target. The target is already authorized or the name is not unique."); }

            if (type == TextChannelType.DirectMessage)
            {
                try { await user!.SendMessageAsync(components: discordUiService.CreateStandardContainer("Authorization granted", $"Hello {user.Mention},\n\nYou have been authorized as a notification beneficiary for `{client.Name}`.\n\n-# ⚠️ Handled information may be sensitive.", AppColors.Warning)); } catch { }
            }

            await context.Interaction.FollowupAsync(components: discordUiService.CreateStandardContainer("Target authorized", $"**Client name:** {clientName}\n**Name:** `{friendlyName}`\n**ID:** `{targetId}`\n**Type:** `{type.GetDiscordLabel()}`\n**Auto-Crosspost:** `{autoCrosspost}`"), flags: MessageFlags.ComponentsV2, ephemeral: true);
        }

        public async Task ManageTargetAsync(AppInteractionContext context, string clientName, string rawTargetId)
        {
            var client = await apiClientRepository.GetByNameAsync(clientName) ?? throw new UserVisibleException($"API Client `{clientName}` not found.");
            if (!ulong.TryParse(rawTargetId, out var targetDiscordId)) throw new UserVisibleException("Invalid target format.");

            var target = await targetRepository.GetByDiscordIdAsync(client.Id, targetDiscordId) ?? throw new UserVisibleException("Target not found.");
            await context.Interaction.RespondAsync(components: BuildTargetOverview(context, client, target), ephemeral: true, flags: MessageFlags.ComponentsV2);
        }

        public async Task ProcessTargetActionAsync(AppInteractionContext context, long clientId, ulong targetDiscordId, string actionId, string[]? selectedValues)
        {
            var client = await apiClientRepository.GetByIdAsync(clientId) ?? throw new UserVisibleException("Client not found.");
            var target = await targetRepository.GetByDiscordIdAsync(clientId, targetDiscordId) ?? throw new UserVisibleException("Target not found.");
            string action = selectedValues?.Length > 0 ? selectedValues[0] : actionId;

            switch (action)
            {
                case nameof(AllowedTargetModifyingAction.ChangeFriendlyName):
                    await context.Interaction.RespondWithModalAsync(discordUiService.CreateSingleInputModal($"target_modal_rename:{clientId}:{targetDiscordId}", "Rename Target", "New Display Name", "Enter new unique name...", 50));
                    break;

                case nameof(AllowedTargetModifyingAction.ChangeCrosspostMode):
                    await UpdateWithSubmenuAsync(context, client, target, discordUiService.GetCrosspostSelectMenuBuilder($"target_select:{clientId}:{targetDiscordId}:crosspost", target.AutoCrosspost));
                    break;

                case nameof(AllowedTargetModifyingAction.SynchronizeTargetData):
                    await UpdateWithWarningAsync(context, client, target, "⚠️ `WARNING`\nThis action will force a resynchronization with Discord's current data. If the channel type is no longer supported, the target will be automatically removed.\n\n**Proceed?**", $"target_btn:{clientId}:{targetDiscordId}:sync_confirm");
                    break;

                case nameof(AllowedTargetModifyingAction.Remove):
                    await UpdateWithWarningAsync(context, client, target, "🛑 `WARNING`\nThis will permanently delete this target from the database.\n\n**Proceed?**", $"target_btn:{clientId}:{targetDiscordId}:remove_confirm");
                    break;

                case "cp_true":
                case "cp_false":
                    target.AutoCrosspost = action == "cp_true";
                    await targetRepository.UpdateAsync(target);
                    await RefreshUiAsync(context, client, target, $"Auto-Publish mode has been updated to **{target.AutoCrosspost}**.");
                    break;

                case "sync_confirm":
                    await context.Interaction.DeferAsync(ephemeral: true);
                    IChannel? rChannel = target.ChannelType != TextChannelType.DirectMessage ? (context.Client.GetChannel(targetDiscordId) as IChannel ?? await context.Client.Rest.GetChannelAsync(targetDiscordId)) : null;
                    IUser? rUser = target.ChannelType == TextChannelType.DirectMessage ? (context.Client.GetUser(targetDiscordId) as IUser ?? await context.Client.Rest.GetUserAsync(targetDiscordId)) : null;

                    var result = await syncService.VerifyAndUpdateTargetAsync(target, rChannel, rUser) ?? throw new UserVisibleException("Failed to synchronize target. It violates the allowed channel types and was automatically removed.");
                    await ((IComponentInteraction)context.Interaction).ModifyOriginalResponseAsync(msg => msg.Components = BuildTargetOverview(context, client, result));
                    await context.Interaction.FollowupAsync("Synchronization complete.", ephemeral: true);
                    break;

                case "remove_confirm":
                    IUser? targetUser = target.ChannelType == TextChannelType.DirectMessage ? await context.Client.Rest.GetUserAsync(target.TargetId) : null;
                    await targetRepository.DeleteByIdAsync(clientId, target.Id);

                    if (targetUser != null)
                        try { await targetUser.SendMessageAsync(components: discordUiService.CreateStandardContainer("Authorization revoked", $"Hello {targetUser.Mention},\n\nYour access as a notification beneficiary for `{client.Name}` has been revoked.", AppColors.Error)); } catch { }

                    await ((IComponentInteraction)context.Interaction).UpdateAsync(msg => msg.Components = discordUiService.CreateStandardContainer("Target Removed", $"The target has been permanently removed from client `{client.Name}`.", Color.Red));
                    break;

                case "cancel":
                default:
                    await RefreshUiAsync(context, client, target);
                    break;
            }
        }

        public async Task HandleTargetRenameModalAsync(AppInteractionContext context, long clientId, ulong targetDiscordId, SingleInputModal modal)
        {
            var target = await targetRepository.GetByDiscordIdAsync(clientId, targetDiscordId) ?? throw new UserVisibleException("Target not found.");
            var client = await apiClientRepository.GetByIdAsync(clientId);

            try
            {
                target.Name = modal.Input1.Trim();
                await targetRepository.UpdateAsync(target);
                await RefreshUiAsync(context, client!, target, $"Target successfully renamed to `{target.Name}`.");
            }
            catch (DbUpdateException) { throw new UserVisibleException("Failed to rename target. The name is already used."); }
        }

        private MessageComponent BuildTargetOverview(AppInteractionContext context, IntegrationClients client, KnownDeliveryTargets target)
        {
            var userPermissions = context.Admin!.Role.RolePermissions.Select(rp => rp.Permission.Key).ToList();
            return discordUiService.CreateTargetOverviewContainer(client.Name, target, cb => cb.WithActionRow(row => row.AddComponent(discordUiService.GetTargetManagementMenuBuilder($"target_select:{client.Id}:{target.TargetId}:action", userPermissions))));
        }

        private async Task UpdateInteractionComponentsAsync(AppInteractionContext context, MessageComponent components)
        {
            if (context.Interaction is IComponentInteraction comp)
                await comp.UpdateAsync(msg => msg.Components = components);
            else if (context.Interaction is IModalInteraction modal)
                await modal.UpdateAsync(msg => msg.Components = components);
        }

        private async Task RefreshUiAsync(AppInteractionContext context, IntegrationClients client, KnownDeliveryTargets target, string? followupMessage = null)
        {
            await UpdateInteractionComponentsAsync(context, BuildTargetOverview(context, client, target));
            if (followupMessage != null) await context.Interaction.FollowupAsync(followupMessage, ephemeral: true);
        }

        private async Task UpdateWithWarningAsync(AppInteractionContext context, IntegrationClients client, KnownDeliveryTargets target, string text, string confirmId)
        {
            await UpdateInteractionComponentsAsync(context, discordUiService.CreateTargetOverviewContainer(client.Name, target, cb =>
                cb.WithTextDisplay(text).WithActionRow(row => {
                    row.AddComponent(new ButtonBuilder().WithCustomId(confirmId).WithLabel("Confirm").WithStyle(ButtonStyle.Danger).WithEmote(emoteCache.GetEmote("UI_ICON_CHECK_WHITE")));
                    row.AddComponent(new ButtonBuilder().WithCustomId($"target_btn:{client.Id}:{target.TargetId}:cancel").WithLabel("Return").WithStyle(ButtonStyle.Secondary).WithEmote(emoteCache.GetEmote("UI_ICON_UNDO")));
                })));
        }

        private async Task UpdateWithSubmenuAsync(AppInteractionContext context, IntegrationClients client, KnownDeliveryTargets target, SelectMenuBuilder submenu)
        {
            await UpdateInteractionComponentsAsync(context, discordUiService.CreateTargetOverviewContainer(client.Name, target, cb =>
                cb.WithActionRow(row => row.AddComponent(submenu)).WithActionRow(row => row.AddComponent(new ButtonBuilder().WithCustomId($"target_btn:{client.Id}:{target.TargetId}:cancel").WithLabel("Return").WithStyle(ButtonStyle.Secondary).WithEmote(emoteCache.GetEmote("UI_ICON_UNDO"))))));
        }
    }