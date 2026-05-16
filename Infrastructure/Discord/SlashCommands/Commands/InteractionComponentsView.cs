using Application.Discord.Panels.Core;
using Application.Repositories;
using Discord;
using Discord.Interactions;
using Infrastructure.Attributes;
using Infrastructure.Discord.Autocomplete;
using Infrastructure.Discord.SlashCommands.Commands.Controllers.Api.Client;
using Infrastructure.Discord.SlashCommands.Commands.Controllers.Api.WellKnownTargets;
using Infrastructure.Discord.SlashCommands.Commands.Controllers.System;
using Infrastructure.Discord.SlashCommands.Commands.Controllers.Zabbix;
using Infrastructure.Exceptions;
using Infrastructure.Models.Modals;
using Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Components.RenderTree;
using System;
using System.Threading.Tasks;

namespace Infrastructure.Discord.SlashCommands.Commands;

public class InteractionComponentsView
{
    [Group("api", "Manage API settings")]
    public class ApiCommandsView : InteractionModuleBase<AppInteractionContext>
    {
        [Group("client", "Manage API client settings")]
        public class ClientCommandsView(ClientCommandsController controller) : InteractionModuleBase<AppInteractionContext>
        {
            public IServiceProvider ServiceProvider { get; set; } = null!;

            [RequirePermission("api.clients.write")]
            [SlashCommand("create", "Creates a new API client and returns the generated API key")]
            public async Task CreateApiClientAsync(string clientName, string zabbixApiUrl, string zabbixApiToken)
                => await controller.CreateApiClientAsync(Context, clientName, zabbixApiUrl, zabbixApiToken);

            [RequirePermission("api.clients.read")]
            [SlashCommand("manage", "Opens the management panel for an API client")]
            public async Task ManageApiClientAsync(
                [Summary("client", "Start typing to search for an API client...")]
                [Autocomplete(typeof(ApiClientAutocompleteHandler))] string clientName)
                => await controller.ManageApiClientAsync(Context, ServiceProvider, clientName);
        }

        [Group("known-target", "Manage well-known targets for API clients")]
        public class WellKnownTargetsView(WellKnownTargetsController controller) : InteractionModuleBase<AppInteractionContext>
        {
            [RequirePermission("api.knownTargets.write")]
            [SlashCommand("create", "Create a new well-known target for an API client")]
            public async Task AddTargetAsync(
                [Summary("client", "Start typing to search for an active API client...")][Autocomplete(typeof(ApiClientAutocompleteHandler))] string clientName,
                [Summary("friendly-name", "A descriptive name for this target (1-50 chars)")][MinLength(1), MaxLength(50)] string friendlyName,
                [Summary("channel", "Select a channel/thread to authorize")] IChannel? channel = null,
                [Summary("user", "Select a user to authorize for Direct Messages")] IUser? user = null,
                [Summary("auto_crosspost", "If announcement channel, automatically publish messages?")] bool autoCrosspost = false)
                => await controller.AddTargetAsync(Context, clientName, friendlyName, channel, user, autoCrosspost);

            [RequirePermission("api.knownTargets.read")]
            [SlashCommand("manage", "Opens the management panel for a specific target")]
            public async Task ManageTargetAsync(
                [Summary("client", "The API client to search within")][Autocomplete(typeof(ApiClientAutocompleteHandler))] string clientName,
                [Summary("target", "The specific Discord target to manage")][Autocomplete(typeof(ApiTargetAutocompleteHandler))] string rawTargetId)
                => await controller.ManageTargetAsync(Context, clientName, rawTargetId);

            // Buttons Router
            [RequirePermission("api.knownTargets.read")]
            [ComponentInteraction("target_btn:*:*:*", ignoreGroupNames: true)]
            public async Task HandleTargetButtonAsync(long clientId, ulong targetDiscordId, string actionId)
                => await controller.ProcessTargetActionAsync(Context, clientId, targetDiscordId, actionId, null);

            // Select Menus Router
            [RequirePermission("api.knownTargets.read")]
            [ComponentInteraction("target_select:*:*:*", ignoreGroupNames: true)]
            public async Task HandleTargetSelectAsync(long clientId, ulong targetDiscordId, string actionId, string[] selectedValues)
                => await controller.ProcessTargetActionAsync(Context, clientId, targetDiscordId, actionId, selectedValues);

            // Modals Router
            [RequirePermission("api.knownTargets.write")]
            [ModalInteraction("target_modal_rename:*:*", ignoreGroupNames: true)]
            public async Task HandleTargetRenameModalAsync(long clientId, ulong targetDiscordId, SingleInputModal modal)
                => await controller.HandleTargetRenameModalAsync(Context, clientId, targetDiscordId, modal);
        }
    }

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

    public class ZabbixDirectMessageView(ZabbixDirectMessageController controller) : InteractionModuleBase<AppInteractionContext>
    {
        // Buttons Router
        [ComponentInteraction("zabbix_btn:*:*:*", ignoreGroupNames: true)]
        public async Task HandleZabbixButtonAsync([RequireActiveApiClient] long apiId, string eventId, string actionId)
            => await controller.ProcessZabbixActionAsync(Context, apiId, eventId, actionId, null);

        // Select Menus Router
        [ComponentInteraction("zabbix_select:*:*:*", ignoreGroupNames: true)]
        public async Task HandleZabbixSelectAsync([RequireActiveApiClient] long apiId, string eventId, string actionId, string[] selectedValues)
            => await controller.ProcessZabbixActionAsync(Context, apiId, eventId, actionId, selectedValues);

        // Modals Router
        [ModalInteraction("zabbix_modal_comment:*:*", ignoreGroupNames: true)]
        public async Task HandleZabbixCommentModalAsync([RequireActiveApiClient] long apiId, string eventId, SingleInputModal modal)
            => await controller.HandleZabbixCommentModalAsync(Context, apiId, eventId, modal);
    }
}