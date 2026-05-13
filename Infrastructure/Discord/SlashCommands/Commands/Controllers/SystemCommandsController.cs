using Application.Repositories;
using Application.Services.Discord;
using Application.Services.Pagination;
using Discord;
using Discord.Interactions;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Exceptions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Discord.SlashCommands.Commands
{
    public class AdministrationCommandsController(
        ILogger<AdministrationCommandsController> logger,
        ISystemAdministratorRepository adminRepository,
        IDiscordUiService discordUiService,
        IPaginationService paginationService,
        IDiscordEmoteService emoteCache,
        IDbContextFactory<ApiSecurityDbContext> dbFactory)
    {
        public async Task CreateAdministratorAsync(AppInteractionContext context, IUser user, int roleId)
        {
            await context.Interaction.DeferAsync(ephemeral: true);
            await using var db = await dbFactory.CreateDbContextAsync();
            var selectedRole = await db.SystemRoles.FindAsync(roleId) ?? throw new UserVisibleException("Role ID does not exist.");

            if (context.Admin!.Role.HierarchyWeight <= selectedRole.HierarchyWeight) throw new UserVisibleException("Cannot assign roles higher or equal to your hierarchy weight.");
            if (user.Id == context.User.Id || user.IsBot) throw new UserVisibleException("Invalid user target.");
            if (await adminRepository.GetByDiscordIdAsync(user.Id) != null) throw new UserVisibleException($"User is already registered.");

            var newAdmin = new SystemAdministrators { DiscordUserId = user.Id, CreatedById = context.Admin.Id, RoleId = roleId, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
            await adminRepository.AddAsync(newAdmin);

            try { await user.SendMessageAsync(components: discordUiService.CreateStandardContainer("Welcome!", $"Hello <@{user.Id}>,\nYou have been granted `{selectedRole.Name}` role.", AppColors.Success)); } catch { }
            await context.Interaction.FollowupAsync(components: discordUiService.CreateStandardContainer("Created", $"User <@{user.Id}> added.\n-# Role: {selectedRole.Name}", AppColors.Success), ephemeral: true, flags: MessageFlags.ComponentsV2);
            logger.LogInformation("Admin {CreatorId} created new user {NewUserId} with Role ID {Role}", context.User.Id, user.Id, roleId);
        }

        public async Task ListAdministratorsAsync(AppInteractionContext context)
        {
            await context.Interaction.DeferAsync(ephemeral: true);
            var dbAdmins = await adminRepository.GetAllAsync();
            var items = new List<string>();

            foreach (var admin in dbAdmins.OrderByDescending(a => a.Role.HierarchyWeight))
            {
                IUser? dUser = context.Client.GetUser(admin.DiscordUserId) as IUser ?? await context.Client.Rest.GetUserAsync(admin.DiscordUserId);
                string uName = dUser != null ? $"**{dUser.Username}**" : "*Unknown User*";
                string statusIcon = admin.IsActive ? emoteCache.GetEmote("UI_ICON_ACTIVE")!.ToString() : emoteCache.GetEmote("UI_ICON_INACTIVE")!.ToString();
                items.Add($"{uName} (`{admin.DiscordUserId}`)\n-# ├ **Role:** `{admin.Role.Name}`\n-# ├ **Status:** {statusIcon}\n-# └ **Protected:** {(admin.IsSystemManaged ? "Yes" : "No")}");
            }

            string sessionId = paginationService.CreatePaginationSession($"System Administrators\n-# Total: {dbAdmins.Count}", items, 1200, "\n\n");
            var sessionData = paginationService.GetSessionData(sessionId) ?? throw new UserVisibleException("Failed to generate list.");
            await context.Interaction.FollowupAsync(components: discordUiService.CreatePaginatedContainer(sessionData.Header, sessionData.Pages[0], 1, sessionData.Pages.Count, sessionId), ephemeral: true, flags: MessageFlags.ComponentsV2);
        }

        public async Task ManageAdministratorAsync(AppInteractionContext context, IUser targetUser)
        {
            var targetAdmin = await adminRepository.GetByDiscordIdAsync(targetUser.Id) ?? throw new UserVisibleException("Not an administrator.");
            await context.Interaction.RespondAsync(components: BuildAdminOverview(context, targetAdmin, targetUser), ephemeral: true, flags: MessageFlags.ComponentsV2);
        }

        public async Task ProcessAdminActionAsync(AppInteractionContext context, ulong targetDiscordId, string actionId, string[]? selectedValues)
        {
            var targetAdmin = await adminRepository.GetByDiscordIdAsync(targetDiscordId) ?? throw new UserVisibleException("Administrator not found.");
            if (context.User.Id == targetDiscordId) throw new UserVisibleException("Cannot modify own status.");
            if (context.Admin!.Role.HierarchyWeight <= targetAdmin.Role.HierarchyWeight) throw new UserVisibleException("Cannot manage user with higher or equal hierarchy.");

            IUser? dUser = context.Client.GetUser(targetDiscordId) as IUser ?? await context.Client.Rest.GetUserAsync(targetDiscordId);
            string action = selectedValues?.Length > 0 ? selectedValues[0] : actionId;

            switch (action)
            {
                case nameof(BotAdminAction.ChangeUserRole):
                    await using (var db = await dbFactory.CreateDbContextAsync())
                    {
                        var assignableRoles = await db.SystemRoles.Where(r => r.HierarchyWeight < context.Admin.Role.HierarchyWeight).OrderByDescending(r => r.HierarchyWeight).ToListAsync();
                        if (assignableRoles.Count == 0) throw new UserVisibleException("No roles available.");
                        await UpdateWithSubmenuAsync(context, targetAdmin, dUser!, discordUiService.GetSystemRoleMenuBuilder($"admin_select:{targetDiscordId}:set_role", targetAdmin.RoleId, assignableRoles));
                    }
                    break;

                case nameof(BotAdminAction.EnableOrDisableUser):
                    await UpdateWithSubmenuAsync(context, targetAdmin, dUser!, discordUiService.GetAdminStatusMenuBuilder($"admin_select:{targetDiscordId}:status", targetAdmin.IsActive));
                    break;

                case "status_true":
                case "status_false":
                    targetAdmin.IsActive = action == "status_true";
                    await adminRepository.UpdateAsync(targetAdmin);
                    try { await dUser!.SendMessageAsync(components: discordUiService.CreateStandardContainer("Account Update", $"Your account is now {(targetAdmin.IsActive ? "ACTIVE" : "SUSPENDED")}.", targetAdmin.IsActive ? AppColors.Success : AppColors.Error)); } catch { }
                    await RefreshUiAsync(context, targetAdmin, dUser!, $"Status updated to **{(targetAdmin.IsActive ? "ACTIVE" : "DISABLED")}**.");
                    break;

                case "set_role":
                    int newRoleId = int.Parse(selectedValues![0]);
                    await using (var db = await dbFactory.CreateDbContextAsync())
                    {
                        var newRole = await db.SystemRoles.FindAsync(newRoleId) ?? throw new UserVisibleException("Role does not exist.");
                        if (context.Admin.Role.HierarchyWeight <= newRole.HierarchyWeight) throw new UserVisibleException("Cannot assign higher role.");
                        targetAdmin.RoleId = newRoleId;
                        await adminRepository.UpdateAsync(targetAdmin);
                        try { await dUser!.SendMessageAsync(components: discordUiService.CreateStandardContainer("Role Updated", $"Your role is now `{newRole.Name}`.", AppColors.Warning)); } catch { }
                    }
                    var refreshedAdmin = await adminRepository.GetByDiscordIdAsync(targetDiscordId);
                    await RefreshUiAsync(context, refreshedAdmin!, dUser!, "Role updated.");
                    break;

                case "cancel":
                default:
                    await RefreshUiAsync(context, targetAdmin, dUser!);
                    break;
            }
        }

        private MessageComponent BuildAdminOverview(AppInteractionContext context, SystemAdministrators targetAdmin, IUser discordUser)
        {
            return discordUiService.CreateAdminOverviewContainer(targetAdmin, discordUser, cb => cb.WithActionRow(row => row.AddComponent(discordUiService.GetAdminActionMenuBuilder($"admin_select:{targetAdmin.DiscordUserId}:action", targetAdmin, context.Admin!))));
        }

        private async Task UpdateInteractionComponentsAsync(AppInteractionContext context, MessageComponent components)
        {
            if (context.Interaction is IComponentInteraction comp)
                await comp.UpdateAsync(msg => msg.Components = components);
            else if (context.Interaction is IModalInteraction modal)
                await modal.UpdateAsync(msg => msg.Components = components);
        }

        private async Task RefreshUiAsync(AppInteractionContext context, SystemAdministrators targetAdmin, IUser discordUser, string? followupMessage = null)
        {
            await UpdateInteractionComponentsAsync(context, BuildAdminOverview(context, targetAdmin, discordUser));
            if (followupMessage != null) await context.Interaction.FollowupAsync(followupMessage, ephemeral: true);
        }

        private async Task UpdateWithSubmenuAsync(AppInteractionContext context, SystemAdministrators targetAdmin, IUser discordUser, SelectMenuBuilder submenu)
        {
            await UpdateInteractionComponentsAsync(context, discordUiService.CreateAdminOverviewContainer(targetAdmin, discordUser, cb =>
                cb.WithActionRow(row => row.AddComponent(submenu)).WithActionRow(row => row.AddComponent(new ButtonBuilder().WithCustomId($"admin_btn:{targetAdmin.DiscordUserId}:cancel").WithLabel("Return").WithStyle(ButtonStyle.Secondary).WithEmote(emoteCache.GetEmote("UI_ICON_UNDO"))))));
        }
    }
}