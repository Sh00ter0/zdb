using Discord;
using Discord.Interactions;
using Infrastructure.Attributes;
using Infrastructure.Discord.Autocomplete;
using Infrastructure.Models.Modals;

namespace Infrastructure.Discord.SlashCommands.Commands
{
    [Group("api", "Manage API settings")]
    public class ApiCommandsView : InteractionModuleBase<AppInteractionContext>
    {
        [Group("client", "Manage API client settings")]
        public class ClientCommandsView(ClientCommandsController controller) : InteractionModuleBase<AppInteractionContext>
        {
            [RequirePermission("api.clients.write")]
            [SlashCommand("create", "Creates a new API client and returns the generated API key")]
            public async Task CreateApiClientAsync(string clientName, string zabbixApiUrl, string zabbixApiToken)
                => await controller.CreateApiClientAsync(Context, clientName, zabbixApiUrl, zabbixApiToken);

            [RequirePermission("api.clients.read")]
            [SlashCommand("manage", "Opens the management panel for an API client")]
            public async Task ManageApiClientAsync(
                [Summary("client", "Start typing to search for an API client...")]
                [Autocomplete(typeof(ApiClientAutocompleteHandler))] string clientName)
                => await controller.ManageApiClientAsync(Context, clientName);

            // Buttons Router
            [RequirePermission("api.clients.write")]
            [ComponentInteraction("client_btn:*:*", ignoreGroupNames: true)]
            public async Task HandleClientButtonAsync(long clientId, string actionId)
                => await controller.ProcessClientActionAsync(Context, clientId, actionId, null);

            // Select Menus Router
            [RequirePermission("api.clients.write")]
            [ComponentInteraction("client_select:*:*", ignoreGroupNames: true)]
            public async Task HandleClientSelectAsync(long clientId, string actionId, string[] selectedValues)
                => await controller.ProcessClientActionAsync(Context, clientId, actionId, selectedValues);

            // Modals Router
            [RequirePermission("api.clients.write")]
            [ModalInteraction("client_modal_rename:*", ignoreGroupNames: true)]
            public async Task HandleClientRenameModalAsync(long clientId, SingleInputModal modal)
                => await controller.HandleClientRenameModalAsync(Context, clientId, modal);

            [RequirePermission("api.clients.write")]
            [ModalInteraction("client_modal_zabbix:*", ignoreGroupNames: true)]
            public async Task HandleClientZabbixModalAsync(long clientId, DualInputModal modal)
                => await controller.HandleClientZabbixModalAsync(Context, clientId, modal);
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
}