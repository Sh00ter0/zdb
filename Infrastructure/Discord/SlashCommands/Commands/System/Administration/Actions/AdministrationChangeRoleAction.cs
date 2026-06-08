using Application.Repositories;
using Application.Services.Discord;
using Discord;
using Discord.Interactions;
using Domain.Constants;
using Domain.Entities;
using Infrastructure.Exceptions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Discord.SlashCommands.Commands.System.Administration.Actions;

public sealed class AdministrationChangeRoleAction(
    ISystemAdministratorRepository adminRepository,
    IDiscordUiService discordUiService,
    IDiscordEmoteService emoteCache,
    IDbContextFactory<ApiSecurityDbContext> dbFactory,
    AdministrationUiBuilder uiBuilder,
    AdministrationPanelRenderer panelRenderer)
{
    public async Task ShowPanelAsync(DiscordInteractionView module,
        SystemAdministrators targetAdmin, IUser targetDiscordUser)
    {
        var undoEmote = emoteCache.GetEmote("UI_ICON_UNDO");

        await using var db = await dbFactory.CreateDbContextAsync();
        var assignableRoles = await db.SystemRoles
            .Where(r => r.HierarchyWeight < module.Context.Admin!.Role.HierarchyWeight)
            .OrderByDescending(r => r.HierarchyWeight)
            .ToListAsync();

        if (assignableRoles.Count == 0) throw new Exceptions.InteractionException("There are no roles available for you to assign.");

        var roleComponents = uiBuilder.CreateOverviewContainer(targetAdmin, targetDiscordUser, cb =>
        {
            cb.WithActionRow(row => row.AddComponent(uiBuilder.GetSystemRoleMenuBuilder($"admin_set_role:{targetAdmin.DiscordUserId}", targetAdmin.RoleId, assignableRoles)));
            cb.WithActionRow(row => row.AddComponent(new ButtonBuilder().WithCustomId($"admin_btn_cancel:{targetAdmin.DiscordUserId}").WithLabel("Return").WithStyle(ButtonStyle.Secondary).WithEmote(undoEmote)));
        });

        await ((IComponentInteraction)module.Context.Interaction).UpdateAsync(msg => msg.Components = roleComponents);
    }

    public async Task HandleSelectAsync(DiscordInteractionView module, ulong targetDiscordId,
        string[] selectedValues)
    {
        var newRoleId = int.Parse(selectedValues[0]);
        var targetAdmin = await adminRepository.GetByDiscordIdAsync(targetDiscordId);
        if (targetAdmin == null) throw new Exceptions.InteractionException("Administrator not found.");

        if (module.Context.User.Id == targetDiscordId)
            throw new Exceptions.InteractionException("Unauthorized action.");

        if (module.Context.Admin!.Role.HierarchyWeight <= targetAdmin.Role.HierarchyWeight)
            throw new Exceptions.InteractionException("You can only change permissions of users with a hierarchy strictly lower than your own.");

        await using var db = await dbFactory.CreateDbContextAsync();
        var newRole = await db.SystemRoles.FindAsync(newRoleId);
        if (newRole == null) throw new Exceptions.InteractionException("The selected role does not exist.");

        if (module.Context.Admin!.Role.HierarchyWeight <= newRole.HierarchyWeight)
            throw new Exceptions.InteractionException("You cannot assign a role with a hierarchy weight equal to or higher than your own.");

        targetAdmin.RoleId = newRoleId;
        var success = await adminRepository.UpdateAsync(targetAdmin);
        if (!success) throw new Exceptions.InteractionException("Database error occurred while updating the role.");

        if (success)
        {
            var messageContainer = discordUiService.CreateStandardContainer(
                header: "Role Updated",
                body: $"Your system role has been changed to `{newRole.Name}` by {module.Context.User.Mention}.",
                accentColor: AppColors.Warning);

            try
            {
                var messageTarget = await module.Context.Client.Rest.GetUserAsync(targetDiscordId);
                if (messageTarget != null)
                {
                    await messageTarget.SendMessageAsync(components: messageContainer);
                }
            }
            catch
            {
            }
        }

        var result = await panelRenderer.CreateManagementPanelAsync(module.Context, targetDiscordId);

        await ((IComponentInteraction)module.Context.Interaction).UpdateAsync(msg => msg.Components = result.Components);
        await module.FollowupInteractionAsync($"Role updated to **{newRole.Name}**.", ephemeral: true);
    }
}
