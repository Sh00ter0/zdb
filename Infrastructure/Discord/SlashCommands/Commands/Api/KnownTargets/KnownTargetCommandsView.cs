using Discord;
using Discord.Interactions;
using Infrastructure.Attributes;
using Infrastructure.Discord.Autocomplete;
using Infrastructure.Discord.SlashCommands.Commands.Api.KnownTargets;
using Infrastructure.Models.Modals;

namespace Infrastructure.Discord.SlashCommands.Commands.Api;

public partial class ApiCommandsGroup
{
    [Group("known-target", "Manage well-known targets for API clients")]
    public sealed class KnownTargetCommandsView(KnownTargetCommandsBackend backend) : DiscordInteractionView
    {
        [RequirePermission("api.knownTargets.write")]
        [SlashCommand("create", "Create a new well-known target for an API client")]
        public Task AddTargetAsync(
            [Summary("client", "Start typing to search for an active API client...")]
            [Autocomplete(typeof(ApiClientAutocompleteHandler))] string clientName,

            [Summary("friendly-name", "A descriptive name for this target (1-50 chars)")]
            [MinLength(1), MaxLength(50)] string friendlyName,

            [Summary("channel", "Select a channel/thread to authorize")] IChannel? channel = null,
            [Summary("user", "Select a user to authorize for Direct Messages")] IUser? user = null,
            [Summary("auto_crosspost", "If announcement channel, automatically publish messages?")] bool autoCrosspost = false)
        {
            return backend.AddTargetAsync(this, clientName, friendlyName, channel, user, autoCrosspost);
        }

        [RequirePermission("api.knownTargets.read")]
        [SlashCommand("manage", "Opens the management panel for a specific target")]
        public Task ManageTargetAsync(
            [Summary("client", "The API client to search within")]
            [Autocomplete(typeof(ApiClientAutocompleteHandler))] string clientName,

            [Summary("target", "The specific Discord target to manage")]
            [Autocomplete(typeof(ApiTargetAutocompleteHandler))] string rawTargetId)
        {
            return backend.ManageTargetAsync(this, clientName, rawTargetId);
        }

        [RequirePermission("api.knownTargets.read")]
        [ComponentInteraction("target_select_action:*:*", ignoreGroupNames: true)]
        public Task HandleTargetActionSelectAsync(long clientId, ulong targetDiscordId, string[] selectedValues)
        {
            return backend.HandleTargetActionSelectAsync(this, clientId, targetDiscordId, selectedValues);
        }

        [RequirePermission("api.knownTargets.read")]
        [ComponentInteraction("target_btn_cancel:*:*", ignoreGroupNames: true)]
        public Task HandleCancelManageAsync(long clientId, ulong targetDiscordId)
        {
            return backend.HandleCancelManageAsync(this, clientId, targetDiscordId);
        }

        [RequirePermission("api.knownTargets.write")]
        [ModalInteraction("target_modal_rename:*:*", ignoreGroupNames: true)]
        public Task HandleRenameModalAsync(long clientId, ulong targetDiscordId, ClientActionModal modal)
        {
            return backend.HandleRenameModalAsync(this, clientId, targetDiscordId, modal);
        }

        [RequirePermission("api.knownTargets.write")]
        [ComponentInteraction("target_select_crosspost:*:*", ignoreGroupNames: true)]
        public Task HandleCrosspostSelectAsync(long clientId, ulong targetDiscordId, string[] selectedValues)
        {
            return backend.HandleCrosspostSelectAsync(this, clientId, targetDiscordId, selectedValues);
        }

        [RequirePermission("api.knownTargets.write")]
        [ComponentInteraction("target_btn_sync_confirm:*:*", ignoreGroupNames: true)]
        public Task HandleSyncConfirmAsync(long clientId, ulong targetDiscordId)
        {
            return backend.HandleSyncConfirmAsync(this, clientId, targetDiscordId);
        }

        [RequirePermission("api.knownTargets.write")]
        [ComponentInteraction("target_btn_remove_confirm:*:*", ignoreGroupNames: true)]
        public Task HandleRemoveConfirmAsync(long clientId, ulong targetDiscordId)
        {
            return backend.HandleRemoveConfirmAsync(this, clientId, targetDiscordId);
        }
    }
}
