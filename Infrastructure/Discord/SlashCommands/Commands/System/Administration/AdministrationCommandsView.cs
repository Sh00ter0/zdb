using Discord;
using Discord.Interactions;
using Infrastructure.Attributes;
using Infrastructure.Discord.SlashCommands.Commands.System.Administration;

namespace Infrastructure.Discord.SlashCommands.Commands.System;

public partial class SystemCommandsGroup
{
    [Group("administration", "Commands for managing system administration")]
    public sealed class AdministrationCommandsView(AdministrationCommandsBackend backend) : DiscordInteractionView
    {
        [RequirePermission("system.admins.write")]
        [SlashCommand("create-administrator", "Registers a new system administrator.")]
        public Task CreateAdministratorAsync(
            [Summary("user", "Select the user to promote")] IUser user,
            [Summary("role_id", "ID of the role from the database (e.g. 2 for Admin, 3 for Moderator)")] int roleId)
        {
            return backend.CreateAdministratorAsync(this, user, roleId);
        }

        [RequirePermission("system.admins.read")]
        [SlashCommand("list", "Displays a paginated list of all system administrators.")]
        public Task ListAdministratorsAsync()
        {
            return backend.ListAdministratorsAsync(this);
        }

        [RequirePermission("system.admins.read")]
        [SlashCommand("manage-administrator", "Opens the management panel for an administrator.")]
        public Task ManageAdministratorAsync([Summary("user", "Select the administrator to manage")] IUser targetUser)
        {
            return backend.ManageAdministratorAsync(this, targetUser);
        }

        [RequirePermission("system.admins.read")]
        [ComponentInteraction("admin_select_action:*", ignoreGroupNames: true)]
        public Task HandleAdminActionSelectAsync(ulong targetDiscordId, string[] selectedValues)
        {
            return backend.HandleAdminActionSelectAsync(this, targetDiscordId, selectedValues);
        }

        [RequirePermission("system.admins.write")]
        [ComponentInteraction("admin_set_role:*", ignoreGroupNames: true)]
        public Task HandleSetRoleAsync(ulong targetDiscordId, string[] selectedValues)
        {
            return backend.HandleSetRoleAsync(this, targetDiscordId, selectedValues);
        }

        [RequirePermission("system.admins.write")]
        [ComponentInteraction("admin_set_status:*", ignoreGroupNames: true)]
        public Task HandleSetStatusAsync(ulong targetDiscordId, string[] selectedValues)
        {
            return backend.HandleSetStatusAsync(this, targetDiscordId, selectedValues);
        }

        [RequirePermission("system.admins.read")]
        [ComponentInteraction("admin_btn_cancel:*", ignoreGroupNames: true)]
        public Task HandleAdminCancelAsync(ulong targetDiscordId)
        {
            return backend.HandleAdminCancelAsync(this, targetDiscordId);
        }
    }
}
