using Discord;
using Discord.Interactions;
using Infrastructure.Attributes;

namespace Infrastructure.Discord.SlashCommands.Commands
{
    [Group("system", "System-level commands and interactions")]
    public class SystemCommandsView : InteractionModuleBase<AppInteractionContext>
    {
        [Group("administration", "Commands for managing system administration")]
        public class AdministrationCommandsView(AdministrationCommandsController controller) : InteractionModuleBase<AppInteractionContext>
        {
            [RequirePermission("system.admins.write")]
            [SlashCommand("create-administrator", "Registers a new system administrator.")]
            public async Task CreateAdministratorAsync(IUser user, int roleId)
                => await controller.CreateAdministratorAsync(Context, user, roleId);

            [RequirePermission("system.admins.read")]
            [SlashCommand("list", "Displays a paginated list of all system administrators.")]
            public async Task ListAdministratorsAsync()
                => await controller.ListAdministratorsAsync(Context);

            [RequirePermission("system.admins.read")]
            [SlashCommand("manage-administrator", "Opens the management panel for an administrator.")]
            public async Task ManageAdministratorAsync(IUser targetUser)
                => await controller.ManageAdministratorAsync(Context, targetUser);

            // Buttons Router
            [RequirePermission("system.admins.read")]
            [ComponentInteraction("admin_btn:*:*", ignoreGroupNames: true)]
            public async Task HandleAdminButtonAsync(ulong targetDiscordId, string actionId)
                => await controller.ProcessAdminActionAsync(Context, targetDiscordId, actionId, null);

            // Select Menus Router
            [RequirePermission("system.admins.read")]
            [ComponentInteraction("admin_select:*:*", ignoreGroupNames: true)]
            public async Task HandleAdminSelectAsync(ulong targetDiscordId, string actionId, string[] selectedValues)
                => await controller.ProcessAdminActionAsync(Context, targetDiscordId, actionId, selectedValues);
        }
    }
}