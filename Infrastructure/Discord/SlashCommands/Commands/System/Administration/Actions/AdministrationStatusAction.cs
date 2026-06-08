using Application.Repositories;
using Application.Services.Discord;
using Discord;
using Discord.Interactions;
using Domain.Constants;
using Domain.Entities;
using Infrastructure.Exceptions;

namespace Infrastructure.Discord.SlashCommands.Commands.System.Administration.Actions;

public sealed class AdministrationStatusAction(
    ISystemAdministratorRepository adminRepository,
    IDiscordUiService discordUiService,
    IDiscordEmoteService emoteCache,
    AdministrationUiBuilder uiBuilder,
    AdministrationPanelRenderer panelRenderer)
{
    public async Task ShowPanelAsync(DiscordInteractionView module,
        SystemAdministrators targetAdmin, IUser targetDiscordUser)
    {
        var undoEmote = emoteCache.GetEmote("UI_ICON_UNDO");
        var statusComponents = uiBuilder.CreateOverviewContainer(targetAdmin, targetDiscordUser, cb =>
        {
            cb.WithActionRow(row => row.AddComponent(uiBuilder.GetStatusMenuBuilder($"admin_set_status:{targetAdmin.DiscordUserId}", targetAdmin.IsActive)));
            cb.WithActionRow(row => row.AddComponent(new ButtonBuilder().WithCustomId($"admin_btn_cancel:{targetAdmin.DiscordUserId}").WithLabel("Return").WithStyle(ButtonStyle.Secondary).WithEmote(undoEmote)));
        });

        await ((IComponentInteraction)module.Context.Interaction).UpdateAsync(msg => msg.Components = statusComponents);
    }

    public async Task HandleSelectAsync(DiscordInteractionView module, ulong targetDiscordId,
        string[] selectedValues)
    {
        var newState = bool.Parse(selectedValues[0]);
        var targetAdmin = await adminRepository.GetByDiscordIdAsync(targetDiscordId);
        if (targetAdmin == null) throw new Exceptions.InteractionException("Administrator not found.");

        if (module.Context.User.Id == targetDiscordId)
            throw new Exceptions.InteractionException("Unauthorized action.");

        if (module.Context.Admin!.Role.HierarchyWeight <= targetAdmin.Role.HierarchyWeight)
            throw new Exceptions.InteractionException("You can only change the status of users with a hierarchy strictly lower than your own.");

        targetAdmin.IsActive = newState;
        var success = await adminRepository.UpdateAsync(targetAdmin);
        if (!success) throw new Exceptions.InteractionException("Database error occurred while updating the status.");

        var targetDiscordUser = (module.Context.Client.GetUser(targetDiscordId) as IUser) ?? await module.Context.Client.Rest.GetUserAsync(targetDiscordId);
        if (targetDiscordUser == null) throw new Exceptions.InteractionException("Could not fetch user from Discord API.");

        var components = panelRenderer.CreateManagementPanel(targetAdmin, targetDiscordUser, module.Context);

        if (newState is true)
        {
            var enableMessageContainer = discordUiService.CreateStandardContainer(
                header: "Account Enabled",
                body: $"Your account has been re-enabled by {module.Context.User.Mention}. You now have access to the system again.",
                accentColor: AppColors.Success);

            try
            {
                await targetDiscordUser.SendMessageAsync(components: enableMessageContainer);
            }
            catch
            {
            }
        }
        else
        {
            var disableMessageContainer = discordUiService.CreateStandardContainer(
                header: "Account Suspended",
                body: $"Your account has been suspended by {module.Context.User.Mention}. You no longer have access to the system.",
                accentColor: AppColors.Error);

            try
            {
                await targetDiscordUser.SendMessageAsync(components: disableMessageContainer);
            }
            catch
            {
            }
        }

        await ((IComponentInteraction)module.Context.Interaction).UpdateAsync(msg => msg.Components = components);
        await module.FollowupInteractionAsync($"Status updated to **{(newState ? "ACTIVE" : "DISABLED")}**.", ephemeral: true);
    }
}
