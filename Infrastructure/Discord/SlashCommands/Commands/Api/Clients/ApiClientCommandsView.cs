using Discord.Interactions;
using Infrastructure.Attributes;
using Infrastructure.Discord.Autocomplete;
using Infrastructure.Discord.SlashCommands.Commands.Api.Clients;
using Infrastructure.Models.Modals;

namespace Infrastructure.Discord.SlashCommands.Commands.Api;

public partial class ApiCommandsGroup
{
    [Group("client", "Manage API client settings")]
    public sealed class ClientCommandsView(ApiClientCommandsBackend backend) : DiscordInteractionView
    {
        [RequirePermission("api.clients.write")]
        [SlashCommand("create", "Creates a new API client and returns the generated API key")]
        public Task CreateApiClientAsync(string clientName, string zabbixApiUrl, string zabbixApiToken)
        {
            return backend.CreateApiClientAsync(this, clientName, zabbixApiUrl, zabbixApiToken);
        }

        [RequirePermission("api.clients.read")]
        [SlashCommand("manage", "Opens the management panel for an API client")]
        public Task ManageApiClientAsync(
            [Summary("client", "Start typing to search for an API client...")]
            [Autocomplete(typeof(ApiClientAutocompleteHandler))] string clientName)
        {
            return backend.ManageApiClientAsync(this, clientName);
        }

        [RequirePermission("api.clients.read")]
        [ComponentInteraction("client_select_action:*", ignoreGroupNames: true)]
        public Task HandleClientActionSelectAsync(long clientId, string[] selectedValues)
        {
            return backend.HandleClientActionSelectAsync(this, clientId, selectedValues);
        }

        [RequirePermission("api.clients.read")]
        [ComponentInteraction("client_btn_cancel:*", ignoreGroupNames: true)]
        public Task HandleClientCancelAsync(long clientId)
        {
            return backend.HandleClientCancelAsync(this, clientId);
        }

        [RequirePermission("api.clients.write")]
        [ModalInteraction("client_modal_rename:*", ignoreGroupNames: true)]
        public Task HandleClientRenameModalAsync(long clientId, ClientActionModal modal)
        {
            return backend.HandleClientRenameModalAsync(this, clientId, modal);
        }

        [RequirePermission("api.clients.write")]
        [ComponentInteraction("client_select_status:*", ignoreGroupNames: true)]
        public Task HandleClientStatusSelectAsync(long clientId, string[] selectedValues)
        {
            return backend.HandleClientStatusSelectAsync(this, clientId, selectedValues);
        }

        [RequirePermission("api.clients.write")]
        [ModalInteraction("client_modal_zabbix:*", ignoreGroupNames: true)]
        public Task HandleClientZabbixModalAsync(long clientId, ZabbixCredentialsModal modal)
        {
            return backend.HandleClientZabbixModalAsync(this, clientId, modal);
        }

        [RequirePermission("api.clients.write")]
        [ComponentInteraction("client_btn_renew_key_confirm:*", ignoreGroupNames: true)]
        public Task HandleClientRenewKeyConfirmAsync(long clientId)
        {
            return backend.HandleClientRenewKeyConfirmAsync(this, clientId);
        }

        [RequirePermission("api.clients.write")]
        [ComponentInteraction("client_btn_remove_confirm:*", ignoreGroupNames: true)]
        public Task HandleClientRemoveConfirmAsync(long clientId)
        {
            return backend.HandleClientRemoveConfirmAsync(this, clientId);
        }
    }
}
