using Application.Repositories;
using Application.Services.Discord;
using Discord;
using Discord.Interactions;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Exceptions;

namespace Infrastructure.Discord.SlashCommands.Commands.Api.KnownTargets.Actions;

public sealed class TargetRemoveAction(
    IIntegrationClientRepository apiClientRepository,
    IKnownDeliveryTargetRepository targetRepository,
    IDiscordUiService discordUiService,
    IDiscordEmoteService emoteCache,
    KnownTargetUiBuilder uiBuilder)
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
                ### 🛑 `WARNING`
                This will permanently delete this target from the database.
                
                **This action is irreversible. Are you sure you want to proceed?**
                """);
            cb.WithActionRow(row =>
            {
                row.AddComponent(new ButtonBuilder().WithCustomId($"target_btn_remove_confirm:{client.Id}:{target.TargetId}").WithLabel("Confirm").WithStyle(ButtonStyle.Danger).WithEmote(confirmEmote));
                row.AddComponent(new ButtonBuilder().WithCustomId($"target_btn_cancel:{client.Id}:{target.TargetId}").WithLabel("Return").WithStyle(ButtonStyle.Secondary).WithEmote(undoEmote));
            });
        });

        await ((IComponentInteraction)module.Context.Interaction).UpdateAsync(msg => msg.Components = components);
    }

    public async Task ConfirmAsync(DiscordInteractionView module, long clientId,
        ulong targetDiscordId)
    {
        var target = await targetRepository.GetByDiscordIdAsync(clientId, targetDiscordId);

        if (target is null) throw new UserVisibleException("Target not found.");

        IUser targetUser = null!;
        if (target.ChannelType is TextChannelType.DirectMessage)
        {
            targetUser = await module.Context.Client.Rest.GetUserAsync(target.TargetId);
        }

        var success = await targetRepository.DeleteByIdAsync(clientId, target.Id);
        if (!success) throw new UserVisibleException("Failed to remove target. It may have already been deleted.");

        if (targetUser != null)
        {
            var userNotification = discordUiService.CreateStandardContainer(
                header: "Authorization to receive notifications revoked",
                body: $"""
                Hello {targetUser.Mention},

                Your access as a notification beneficiary for the API client `{target.IntegrationClient.Name}` has been revoked.
                This means that this client can no longer send you direct messages through the bot.
                
                If you believe this was a mistake or have any questions, please contact {module.Context.User.Mention} for more information.
                """,
                accentColor: AppColors.Error);
            try
            {
                await targetUser.SendMessageAsync(components: userNotification);
            }
            catch
            {
            }
        }

        var client = await apiClientRepository.GetByIdAsync(clientId);
        var clientName = client?.Name ?? "Unknown";

        var deletedComponents = discordUiService.CreateStandardContainer(
            header: "Target Removed",
            body: $"The target has been permanently removed from client `{clientName}`.",
            accentColor: Color.Red);

        await ((IComponentInteraction)module.Context.Interaction).UpdateAsync(msg => msg.Components = deletedComponents);
    }
}
