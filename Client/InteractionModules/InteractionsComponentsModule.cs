using Application.Repositories;
using Application.Services.API;
using Application.Services.Discord;
using Application.Services.Pagination;
using Client.Attributes;
using Client.Contexts;
using Client.Data;
using Client.Extensions;
using Client.Handlers;
using Client.Models;
using Client.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using static Client.InteractionModules.ApiCommandsGroup;

namespace Client.InteractionModules
{
    [Group("api", "Manage API settings")]
    public class ApiCommandsGroup : InteractionModuleBase<AppInteractionContext>
    {
        [Group("client", "Manage API client settings")]
        public class ClientCommandsGroup : InteractionModuleBase<AppInteractionContext>
        {
            private readonly ILogger<ClientCommandsGroup> _logger;
            private readonly IApiSecurityStore _apiSecurityStore;
            private readonly IIntegrationClientRepository _apiClientRepository;
            private readonly IKnownDeliveryTargetRepository _targetRepository;
            private readonly IDiscordUiService _discordUiService;
            private readonly IPaginationService _paginationService;
            private readonly IDiscordEmoteService _emoteCache;

            public ClientCommandsGroup(
                ILogger<ClientCommandsGroup> logger,
                IApiSecurityStore apiSecurityStore,
                IIntegrationClientRepository apiClientRepository,
                IKnownDeliveryTargetRepository targetRepository,
                IDiscordUiService discordUiService,
                IPaginationService paginationService,
                IDiscordEmoteService emoteCacheService)
            {
                _logger = logger;
                _apiSecurityStore = apiSecurityStore;
                _apiClientRepository = apiClientRepository;
                _targetRepository = targetRepository;
                _discordUiService = discordUiService;
                _paginationService = paginationService;
                _emoteCache = emoteCacheService;
            }

            [RequirePermission("api.clients.write")]
            [SlashCommand("create", "Creates a new API client and returns the generated API key")]
            public async Task CreateApiClientAsync(string clientName, string zabbixApiUrl, string zabbixApiToken)
            {
                _logger.LogInformation("Received request to create a new API client. Name: {ClientName}", clientName);
                await DeferAsync(ephemeral: true);

                try
                {
                    var isValidUrl = zabbixApiUrl.IsValidHttpOrHttpsUrl();
                    if (!isValidUrl) throw new UserVisibleException("The provided Zabbix API URL is not valid. Please ensure it starts with http:// or https:// and is properly formatted.");

                    var createdClient = await _apiSecurityStore.CreateApiClientAsync(clientName, zabbixApiUrl, zabbixApiToken);

                    var bodyText = $"""
                                    **Client name:** `{createdClient.Name}`
                                    **Zabbix API URL:** `{zabbixApiUrl}`
                                    **API key:** `{createdClient.ApiKey}`
                                    
                                    ⚠️ **Warning!:** Copy and store this key now. It is only shown once.
                                    """;

                    var components = _discordUiService.CreateStandardContainer(header: "API key created", accentColor: null, body: bodyText);

                    await FollowupAsync(components: components, flags: MessageFlags.ComponentsV2, ephemeral: true);
                    _logger.LogInformation("Successfully created API client and generated key for: {ClientName}", createdClient.Name);
                }
                catch (InvalidOperationException ex)
                {
                    throw new UserVisibleException(ex.Message);
                }
            }

            [RequirePermission("api.clients.read")]
            [SlashCommand("manage", "Opens the management panel for an API client")]
            public async Task ManageApiClientAsync(
            [Summary("client", "Start typing to search for an API client...")]
            [Autocomplete(typeof(ApiClientAutocompleteHandler))] string clientName)
            {
                var client = await _apiClientRepository.GetByNameAsync(clientName);
                if (client is null) throw new UserVisibleException($"API Client `{clientName}` not found.");

                var userPermissions = Context.Admin!.Role.RolePermissions.Select(rp => rp.Permission.Key).ToList();
                var actionMenu = _discordUiService.GetApiClientManagementMenuBuilder($"client_select_action:{client.Id}", userPermissions);

                var components = _discordUiService.CreateApiClientOverviewContainer(client, cb =>
                {
                    cb.WithActionRow(row =>
                    {
                        row.AddComponent(actionMenu);
                    });
                });

                await RespondAsync(components: components, ephemeral: true, flags: MessageFlags.ComponentsV2);
            }

            [RequirePermission("api.clients.read")]
            [ComponentInteraction("client_select_action:*", ignoreGroupNames: true)]
            public async Task HandleClientActionSelectAsync(long clientId, string[] selectedValues)
            {
                var action = Enum.Parse<ApiClientModifyingAction>(selectedValues[0]);
                var client = await _apiClientRepository.GetByIdAsync(clientId);
                if (client == null) throw new UserVisibleException("Client not found.");

                switch (action)
                {
                    case ApiClientModifyingAction.ChangeName:
                        {
                            var renameModal = _discordUiService.CreateConfirmationModal(
                            customId: $"client_modal_rename:{clientId}",
                            title: "Rename API Client",
                            inputLabel: "New Display Name",
                            placeholder: "Enter new unique name...",
                            maxLength: 50);
                            await RespondWithModalAsync(renameModal);
                        }
                        break;

                    case ApiClientModifyingAction.EnableOrDisableClient:
                        {
                            var undoEmote = _emoteCache.GetEmote("UI_ICON_UNDO");
                            var statusComponents = _discordUiService.CreateApiClientOverviewContainer(client, cb =>
                            {
                                cb.WithActionRow(row => row.AddComponent(_discordUiService.GetClientStatusSelectMenuBuilder($"client_select_status:{clientId}", client.IsActive)));
                                cb.WithActionRow(row => row.AddComponent(new ButtonBuilder().WithCustomId($"client_btn_cancel:{clientId}").WithLabel("Return").WithStyle(ButtonStyle.Secondary).WithEmote(undoEmote)));
                            });
                            await ((IComponentInteraction)Context.Interaction).UpdateAsync(msg => msg.Components = statusComponents);
                        }
                        break;

                    case ApiClientModifyingAction.RenewZabbixConnection:
                        {
                            var zabbixModal = new ModalBuilder()
                            .WithTitle("Update Zabbix Connection")
                            .WithCustomId($"client_modal_zabbix:{clientId}")
                            .AddTextInput("New Zabbix API URL", "url", TextInputStyle.Short, "https://zabbix.yourdomain.com/api_jsonrpc.php", required: true)
                            .AddTextInput("New Zabbix API Token", "token", TextInputStyle.Short, "Enter new token...", required: true);
                            await RespondWithModalAsync(zabbixModal.Build());
                        }
                        break;

                    case ApiClientModifyingAction.DisplayRelatedTargets:
                        {
                            await DeferAsync(ephemeral: true);

                            var targetEntities = await _targetRepository.GetAllByClientIdAsync(client.Id);
                            var undoEmote = _emoteCache.GetEmote("UI_ICON_UNDO");

                            if (targetEntities.Count == 0)
                            {
                                var emptyContainer = _discordUiService.CreateApiClientOverviewContainer(client, cb =>
                                {
                                    cb.WithTextDisplay("📝 **This client currently has no authorized targets.**");
                                    cb.WithActionRow(row => row.AddComponent(new ButtonBuilder().WithCustomId($"client_btn_cancel:{clientId}").WithLabel("Return").WithStyle(ButtonStyle.Secondary).WithEmote(undoEmote)));
                                });
                                await ((IComponentInteraction)Context.Interaction).ModifyOriginalResponseAsync(msg => msg.Components = emptyContainer);
                                return;
                            }

                            var items = new List<string>();
                            foreach (var t in targetEntities)
                            {
                                var discordTimestamp = $"<t:{((DateTimeOffset)t.CreatedAtUtc).ToUnixTimeSeconds()}:F>";
                                var bodyText = $"`{t.Name}`\n-# ├ **ID:** `{t.TargetId}`\n-# ├ **Type:** `{t.ChannelType.GetDisplayName()}`\n-# └ **Added:** {discordTimestamp}";
                                items.Add(bodyText);
                            }

                            var returnButton = new ButtonBuilder()
                                .WithCustomId($"client_btn_cancel:{clientId}")
                                .WithLabel("Return")
                                .WithStyle(ButtonStyle.Secondary)
                                .WithEmote(undoEmote);

                            string headerText = $"Targets for: {client.Name}\n-# Total targets: {targetEntities.Count}";

                            string sessionId = _paginationService.CreatePaginationSession(
                                header: headerText,
                                items: items,
                                charsPerPage: 1000,
                                separator: "\n\n",
                                customButton: returnButton
                            );

                            var sessionData = _paginationService.GetSessionData(sessionId);

                            if (sessionData == null || sessionData.Pages.Count == 0)
                            {
                                throw new UserVisibleException("Failed to generate target list.");
                            }

                            var listComponents = _discordUiService.CreatePaginatedContainer(
                                header: sessionData.Header,
                                pageText: sessionData.Pages[0],
                                currentPage: 1,
                                totalPages: sessionData.Pages.Count,
                                sessionId: sessionId,
                                customActionBtn: sessionData.CustomButton
                            );

                            await ((IComponentInteraction)Context.Interaction).ModifyOriginalResponseAsync(msg => msg.Components = listComponents);
                        }
                        break;

                    case ApiClientModifyingAction.RenewApiKey:
                        {
                            var confirmEmote = _emoteCache.GetEmote("UI_ICON_CHECK_WHITE");
                            var undoEmote = _emoteCache.GetEmote("UI_ICON_UNDO");

                            var renewComponents = _discordUiService.CreateApiClientOverviewContainer(client, cb =>
                            {
                                cb.WithTextDisplay(
                                    """
                                    ### ⚠️ `WARNING`
                                    Renewing the API Key will **immediately invalidate the current key**. Any external system using the old key will lose access until updated.
                                    
                                    **This action is irreversible. Are you sure you want to proceed?**
                                    """);
                                cb.WithActionRow(row =>
                                {
                                    row.AddComponent(new ButtonBuilder().WithCustomId($"client_btn_renew_key_confirm:{clientId}").WithLabel("Confirm").WithStyle(ButtonStyle.Danger).WithEmote(confirmEmote));
                                    row.AddComponent(new ButtonBuilder().WithCustomId($"client_btn_cancel:{clientId}").WithLabel("Return").WithStyle(ButtonStyle.Secondary).WithEmote(undoEmote));
                                });
                            });
                            await ((IComponentInteraction)Context.Interaction).UpdateAsync(msg => msg.Components = renewComponents);
                        }
                        break;

                    case ApiClientModifyingAction.Remove:
                        {
                            var confirmEmote = _emoteCache.GetEmote("UI_ICON_CHECK_WHITE");
                            var undoEmote = _emoteCache.GetEmote("UI_ICON_UNDO");

                            var removeComponents = _discordUiService.CreateApiClientOverviewContainer(client, cb =>
                            {
                                cb.WithTextDisplay(
                                    """
                                    ### 🛑 `WARNING`
                                    This will permanently delete this client and ALL associated targets from the database.
                                    
                                    **This action is irreversible. Are you sure you want to proceed?**
                                    """);
                                cb.WithActionRow(row =>
                                {
                                    row.AddComponent(new ButtonBuilder().WithCustomId($"client_btn_remove_confirm:{clientId}").WithLabel("Confirm").WithStyle(ButtonStyle.Danger).WithEmote(confirmEmote));
                                    row.AddComponent(new ButtonBuilder().WithCustomId($"client_btn_cancel:{clientId}").WithLabel("Return").WithStyle(ButtonStyle.Secondary).WithEmote(undoEmote));
                                });
                            });
                            await ((IComponentInteraction)Context.Interaction).UpdateAsync(msg => msg.Components = removeComponents);
                        }
                        break;
                }
            }

            [RequirePermission("api.clients.read")]
            [ComponentInteraction("client_btn_cancel:*", ignoreGroupNames: true)]
            public async Task HandleClientCancelAsync(long clientId)
            {
                var client = await _apiClientRepository.GetByIdAsync(clientId);
                if (client == null) throw new UserVisibleException("Client not found.");

                var userPermissions = Context.Admin!.Role.RolePermissions.Select(rp => rp.Permission.Key).ToList();
                var actionMenu = _discordUiService.GetApiClientManagementMenuBuilder($"client_select_action:{clientId}", userPermissions);

                var components = _discordUiService.CreateApiClientOverviewContainer(client, cb =>
                {
                    cb.WithActionRow(row => row.AddComponent(actionMenu));
                });

                await ((IComponentInteraction)Context.Interaction).UpdateAsync(msg => msg.Components = components);
            }

            [RequirePermission("api.clients.write")]
            [ModalInteraction("client_modal_rename:*", ignoreGroupNames: true)]
            public async Task HandleClientRenameModalAsync(long clientId, ClientActionModal modal)
            {
                string newName = modal.ConfirmText.Trim();
                try
                {
                    var client = await _apiClientRepository.GetByIdAsync(clientId);
                    if (client == null) throw new UserVisibleException("Client not found.");

                    client.Name = newName;
                    var success = await _apiClientRepository.UpdateAsync(client);
                    if (!success) throw new UserVisibleException("Failed to update the client.");

                    var userPermissions = Context.Admin!.Role.RolePermissions.Select(rp => rp.Permission.Key).ToList();
                    var actionMenu = _discordUiService.GetApiClientManagementMenuBuilder($"client_select_action:{clientId}", userPermissions);

                    var components = _discordUiService.CreateApiClientOverviewContainer(client, cb =>
                    {
                        cb.WithActionRow(row => row.AddComponent(actionMenu));
                    });

                    await ((IModalInteraction)Context.Interaction).UpdateAsync(msg => msg.Components = components);
                    await FollowupAsync($"Client successfully renamed to `{newName}`.", ephemeral: true);
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateException)
                {
                    throw new UserVisibleException($"Failed to rename client. The name `{newName}` is already used.");
                }
            }

            [RequirePermission("api.clients.write")]
            [ComponentInteraction("client_select_status:*", ignoreGroupNames: true)]
            public async Task HandleClientStatusSelectAsync(long clientId, string[] selectedValues)
            {
                bool newState = bool.Parse(selectedValues[0]);

                var client = await _apiClientRepository.GetByIdAsync(clientId);
                if (client == null) throw new UserVisibleException("Client not found.");

                client.IsActive = newState;
                var success = await _apiClientRepository.UpdateAsync(client);
                if (!success) throw new UserVisibleException("Failed to update client status.");

                var userPermissions = Context.Admin!.Role.RolePermissions.Select(rp => rp.Permission.Key).ToList();
                var actionMenu = _discordUiService.GetApiClientManagementMenuBuilder($"client_select_action:{clientId}", userPermissions);

                var components = _discordUiService.CreateApiClientOverviewContainer(client, cb =>
                {
                    cb.WithActionRow(row => row.AddComponent(actionMenu));
                });

                await ((IComponentInteraction)Context.Interaction).UpdateAsync(msg => msg.Components = components);
                await FollowupAsync($"Client status has been updated to: **{(client.IsActive ? "ACTIVE" : "DISABLED")}**.", ephemeral: true);
            }

            [RequirePermission("api.clients.write")]
            [ModalInteraction("client_modal_zabbix:*", ignoreGroupNames: true)]
            public async Task HandleClientZabbixModalAsync(long clientId, ZabbixCredentialsModal modal)
            {
                await DeferAsync(ephemeral: true);

                var isValidUrl = modal.Url.IsValidHttpOrHttpsUrl();

                if (!isValidUrl) throw new UserVisibleException("The provided Zabbix API URL is not valid. Please ensure it starts with http:// or https:// and is properly formatted.");

                await _apiSecurityStore.UpdateZabbixConnectionAsync(clientId, modal.Url, modal.Token);

                var client = await _apiClientRepository.GetByIdAsync(clientId);

                var userPermissions = Context.Admin!.Role.RolePermissions.Select(rp => rp.Permission.Key).ToList();
                var actionMenu = _discordUiService.GetApiClientManagementMenuBuilder($"client_select_action:{clientId}", userPermissions);

                var components = _discordUiService.CreateApiClientOverviewContainer(client!, cb =>
                {
                    cb.WithActionRow(row => row.AddComponent(actionMenu));
                });

                await ((IModalInteraction)Context.Interaction).ModifyOriginalResponseAsync(msg => msg.Components = components);
                await FollowupAsync("Zabbix connection credentials successfully updated.", ephemeral: true);
            }

            [RequirePermission("api.clients.write")]
            [ComponentInteraction("client_btn_renew_key_confirm:*", ignoreGroupNames: true)]
            public async Task HandleClientRenewKeyConfirmAsync(long clientId)
            {
                await DeferAsync(ephemeral: true);

                var undoEmote = _emoteCache.GetEmote("UI_ICON_UNDO");

                var newKey = await _apiSecurityStore.RenewApiKeyAsync(clientId);
                var client = await _apiClientRepository.GetByIdAsync(clientId);

                var components = _discordUiService.CreateApiClientOverviewContainer(client!, cb =>
                {
                    cb.WithTextDisplay($"🔒 **NEW API KEY GENERATED:**\n`{newKey}`\n\n*Important: Copy and store this key now. It will not be shown again.*");
                    cb.WithActionRow(row => row.AddComponent(new ButtonBuilder().WithCustomId($"client_btn_cancel:{clientId}").WithLabel("Return").WithStyle(ButtonStyle.Secondary).WithEmote(undoEmote)));
                });

                await ((IComponentInteraction)Context.Interaction).ModifyOriginalResponseAsync(msg => msg.Components = components);
            }

            [RequirePermission("api.clients.write")]
            [ComponentInteraction("client_btn_remove_confirm:*", ignoreGroupNames: true)]
            public async Task HandleClientRemoveConfirmAsync(long clientId)
            {
                var success = await _apiClientRepository.DeleteAsync(clientId);
                if (!success) throw new UserVisibleException("Failed to remove client. It may have already been deleted.");

                var deletedComponents = _discordUiService.CreateStandardContainer(
                    header: "Client Removed",
                    body: $"Api client has been permanently removed.",
                    accentColor: Color.Red);

                await ((IComponentInteraction)Context.Interaction).UpdateAsync(msg => msg.Components = deletedComponents);
            }
        }

        [Group("known-target", "Manage well-known targets for API clients")]
        public class WellKnownTargetsCommandsGroup : InteractionModuleBase<AppInteractionContext>
        {
            private readonly ILogger<WellKnownTargetsCommandsGroup> _logger;
            private readonly IApiSecurityStore _apiSecurityStore;
            private readonly IIntegrationClientRepository _apiClientRepository;
            private readonly IKnownDeliveryTargetRepository _targetRepository;
            private readonly IDiscordUiService _discordUiService;
            private readonly IPaginationService _paginationService;
            private readonly IDiscordTargetSyncService _syncService;
            private readonly IDiscordEmoteService _emoteCache;

            public WellKnownTargetsCommandsGroup(
                ILogger<WellKnownTargetsCommandsGroup> logger,
                IApiSecurityStore apiSecurityStore,
                IIntegrationClientRepository apiClientRepository,
                IKnownDeliveryTargetRepository targetRepository,
                IDiscordUiService discordUiService,
                IPaginationService paginationService,
                IDiscordTargetSyncService syncService,
                IDiscordEmoteService emoteCache)
            {
                _logger = logger;
                _apiSecurityStore = apiSecurityStore;
                _apiClientRepository = apiClientRepository;
                _targetRepository = targetRepository;
                _discordUiService = discordUiService;
                _paginationService = paginationService;
                _syncService = syncService;
                _emoteCache = emoteCache;
            }

            [RequirePermission("api.knownTargets.write")]
            [SlashCommand("create", "Create a new well-known target for an API client")]
            public async Task AddTargetAsync(
                [Summary("client", "Start typing to search for an active API client...")]
                [Autocomplete(typeof(ApiClientAutocompleteHandler))] string clientName,

                [Summary("friendly-name", "A descriptive name for this target (1-50 chars)")]
                [MinLength(1), MaxLength(50)] string friendlyName,

                [Summary("channel", "Select a channel/thread to authorize")] IChannel? channel = null,
                [Summary("user", "Select a user to authorize for Direct Messages")] IUser? user = null,
                [Summary("auto_crosspost", "If announcement channel, automatically publish messages?")] bool autoCrosspost = false)
            {
                await DeferAsync(ephemeral: true);

                if (channel == null && user == null) throw new UserVisibleException("You must select either a Channel or a User to authorize.");
                if (channel != null && user != null) throw new UserVisibleException("Please select ONLY ONE option (Channel OR User).");

                ulong targetId = 0;
                TextChannelType type = TextChannelType.Unknown;
                ulong? guildId = null;

                if (user != null)
                {
                    targetId = user.Id;
                    type = TextChannelType.DirectMessage;
                }
                else if (channel != null)
                {
                    targetId = channel.Id;
                    if (channel is IGuildChannel gChannel) guildId = gChannel.GuildId;

                    if (channel is INewsChannel) type = TextChannelType.GuildAnnouncementChannel;
                    if (channel is IForumChannel) throw new UserVisibleException("Forum channels cannot be directly authorized. Please select a thread within the forum to authorize.");
                    else if (channel is SocketThreadChannel thread)
                    {
                        if (thread.ParentChannel is IForumChannel) type = TextChannelType.GuildForumThreadChannel;
                        else if (thread.Type == ThreadType.PrivateThread) type = TextChannelType.GuildPrivateThreadChannel;
                        else type = TextChannelType.GuildPublicThreadChannel;
                    }
                    else if (channel is IThreadChannel) type = TextChannelType.GuildPublicThreadChannel;
                    else if (channel is ITextChannel && channel is not IVoiceChannel && channel is not IStageChannel) type = TextChannelType.GuildTextChannel;
                    else if (channel is IStageChannel) type = TextChannelType.GuildStageVoiceTextChannel;
                    else if (channel is IVoiceChannel) type = TextChannelType.GuildVoiceTextChannel;
                }

                var client = await _apiClientRepository.GetByNameAsync(clientName);
                if (client == null || !client.IsActive) throw new UserVisibleException($"Failed to add target. Active API Client `{clientName}` was not found.");

                var newTarget = new KnownDeliveryTargets
                {
                    IntegrationClientId = client.Id,
                    TargetId = targetId,
                    ChannelType = type,
                    Name = friendlyName,
                    AssociatedGuildId = guildId,
                    CreatedById = Context.Admin!.Id,
                    AutoCrosspost = autoCrosspost,
                    CreatedAtUtc = DateTime.UtcNow
                };

                try
                {
                    var success = await _targetRepository.AddAsync(newTarget);
                    if (!success) throw new UserVisibleException("An unexpected database error occurred while adding the target.");
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateException)
                {
                    throw new UserVisibleException($"Failed to add target. The target has already been authorized or the display name is not unique for `{clientName}`.");
                }

                if (newTarget.ChannelType is TextChannelType.DirectMessage)
                {
                    var userNotification = _discordUiService.CreateStandardContainer(
                        header: "Authorization to receive notifications granted",
                        body: $"""
                        Hello {user!.Mention},

                        You have been authorized to become a notification beneficiary for the API client `{client.Name}`.
                        This means that this client can send you direct messages through the bot, and these messages will be delivered to you as notifications.

                        If it's a mistake or you wish to revoke this access, please contact {Context.User.Mention} immediately.

                        -# ⚠️ Please, keep in mind. Information transmitted through this communication channel may be **confidential** or **sensitive** in nature. Please **handle it with care** and do not share it with unauthorized parties.
                        """,
                        accentColor: AppColors.Warning);

                    try
                    {
                        await user!.SendMessageAsync(components: userNotification);
                    }
                    catch { }
                }

                var bodyText = $"""
                                **Client name:** {clientName}
                                **Name:** `{friendlyName}`
                                **Discord target ID:** `{targetId}`
                                **Type:** `{type.GetDiscordLabel()}`
                                **Auto-Crosspost:** `{autoCrosspost}`
                                """;

                var components = _discordUiService.CreateStandardContainer(header: "Target authorized", body: bodyText);
                await FollowupAsync(components: components, flags: MessageFlags.ComponentsV2, ephemeral: true);
            }

            [RequirePermission("api.knownTargets.read")]
            [SlashCommand("manage", "Opens the management panel for a specific target")]
            public async Task ManageTargetAsync(
            [Summary("client", "The API client to search within")]
            [Autocomplete(typeof(ApiClientAutocompleteHandler))] string clientName,

            [Summary("target", "The specific Discord target to manage")]
            [Autocomplete(typeof(ApiTargetAutocompleteHandler))] string rawTargetId)
            {
                var client = await _apiClientRepository.GetByNameAsync(clientName);
                if (client is null) throw new UserVisibleException($"API Client `{clientName}` not found.");

                if (!ulong.TryParse(rawTargetId, out var targetDiscordId))
                {
                    throw new UserVisibleException("Invalid target format. Please select a valid target from the autocomplete list.");
                }

                var target = await _targetRepository.GetByDiscordIdAsync(client.Id, targetDiscordId);
                if (target == null) throw new UserVisibleException("Target not found.");

                var userPermissions = Context.Admin!.Role.RolePermissions.Select(rp => rp.Permission.Key).ToList();
                var actionMenu = _discordUiService.GetTargetManagementMenuBuilder($"target_select_action:{client.Id}:{targetDiscordId}", userPermissions);

                var components = _discordUiService.CreateTargetOverviewContainer(client.Name, target, cb =>
                {
                    cb.WithActionRow(row => row.AddComponent(actionMenu));
                });

                await RespondAsync(components: components, ephemeral: true, flags: MessageFlags.ComponentsV2);
            }

            [RequirePermission("api.knownTargets.read")]
            [ComponentInteraction("target_select_action:*:*", ignoreGroupNames: true)]
            public async Task HandleTargetActionSelectAsync(long clientId, ulong targetDiscordId, string[] selectedValues)
            {
                var action = Enum.Parse<AllowedTargetModifyingAction>(selectedValues[0]);
                var client = await _apiClientRepository.GetByIdAsync(clientId);
                var target = await _targetRepository.GetByDiscordIdAsync(clientId, targetDiscordId);

                if (client == null || target == null) throw new UserVisibleException("Target or client not found.");

                var confirmEmote = _emoteCache.GetEmote("UI_ICON_CHECK_WHITE");
                var undoEmote = _emoteCache.GetEmote("UI_ICON_UNDO");

                switch (action)
                {
                    case AllowedTargetModifyingAction.ChangeFriendlyName:
                        var renameModal = _discordUiService.CreateConfirmationModal(
                            customId: $"target_modal_rename:{clientId}:{targetDiscordId}",
                            title: "Rename Target",
                            inputLabel: "New Display Name",
                            placeholder: "Enter new unique name...",
                            maxLength: 50);
                        await RespondWithModalAsync(renameModal);
                        break;

                    case AllowedTargetModifyingAction.ChangeCrosspostMode:
                        var cpComponents = _discordUiService.CreateTargetOverviewContainer(client.Name, target, cb =>
                        {
                            cb.WithActionRow(row => row.AddComponent(_discordUiService.GetCrosspostSelectMenuBuilder($"target_select_crosspost:{clientId}:{targetDiscordId}", target.AutoCrosspost)));
                            cb.WithActionRow(row => row.AddComponent(new ButtonBuilder().WithCustomId($"target_btn_cancel:{clientId}:{targetDiscordId}").WithLabel("Return").WithStyle(ButtonStyle.Secondary).WithEmote(undoEmote)));
                        });
                        await ((IComponentInteraction)Context.Interaction).UpdateAsync(msg => msg.Components = cpComponents);
                        break;

                    case AllowedTargetModifyingAction.SynchronizeTargetData:
                        var syncComponents = _discordUiService.CreateTargetOverviewContainer(client.Name, target, cb =>
                        {
                            cb.WithTextDisplay(
                                """
                                ### ⚠️ `WARNING`
                                This action will force a resynchronization with Discord's current data. If the channel type is no longer supported, the target will be automatically removed.
                                
                                **This action is irreversible. Are you sure you want to proceed?**
                                """);
                            cb.WithActionRow(row =>
                            {
                                row.AddComponent(new ButtonBuilder().WithCustomId($"target_btn_sync_confirm:{clientId}:{targetDiscordId}").WithLabel("Confirm").WithStyle(ButtonStyle.Danger).WithEmote(confirmEmote));
                                row.AddComponent(new ButtonBuilder().WithCustomId($"target_btn_cancel:{clientId}:{targetDiscordId}").WithLabel("Return").WithStyle(ButtonStyle.Secondary).WithEmote(undoEmote));
                            });
                        });
                        await ((IComponentInteraction)Context.Interaction).UpdateAsync(msg => msg.Components = syncComponents);
                        break;

                    case AllowedTargetModifyingAction.Remove:
                        var removeComponents = _discordUiService.CreateTargetOverviewContainer(client.Name, target, cb =>
                        {
                            cb.WithTextDisplay(
                                """
                                ### 🛑 `WARNING`
                                This will permanently delete this target from the database.
                                
                                **This action is irreversible. Are you sure you want to proceed?**
                                """);
                            cb.WithActionRow(row =>
                            {
                                row.AddComponent(new ButtonBuilder().WithCustomId($"target_btn_remove_confirm:{clientId}:{targetDiscordId}").WithLabel("Confirm").WithStyle(ButtonStyle.Danger).WithEmote(confirmEmote));
                                row.AddComponent(new ButtonBuilder().WithCustomId($"target_btn_cancel:{clientId}:{targetDiscordId}").WithLabel("Return").WithStyle(ButtonStyle.Secondary).WithEmote(undoEmote));
                            });
                        });
                        await ((IComponentInteraction)Context.Interaction).UpdateAsync(msg => msg.Components = removeComponents);
                        break;
                }
            }

            [RequirePermission("api.knownTargets.read")]
            [ComponentInteraction("target_btn_cancel:*:*", ignoreGroupNames: true)]
            public async Task HandleCancelManageAsync(long clientId, ulong targetDiscordId)
            {
                var target = await _targetRepository.GetByDiscordIdAsync(clientId, targetDiscordId);
                var client = await _apiClientRepository.GetByIdAsync(clientId);
                if (target == null || client == null) throw new UserVisibleException("Target or client not found.");

                var userPermissions = Context.Admin!.Role.RolePermissions.Select(rp => rp.Permission.Key).ToList();
                var actionMenu = _discordUiService.GetTargetManagementMenuBuilder($"target_select_action:{clientId}:{targetDiscordId}", userPermissions);

                var components = _discordUiService.CreateTargetOverviewContainer(client.Name, target, cb =>
                {
                    cb.WithActionRow(row => row.AddComponent(actionMenu));
                });

                await ((IComponentInteraction)Context.Interaction).UpdateAsync(msg => msg.Components = components);
            }

            [RequirePermission("api.knownTargets.write")]
            [ModalInteraction("target_modal_rename:*:*", ignoreGroupNames: true)]
            public async Task HandleRenameModalAsync(long clientId, ulong targetDiscordId, ClientActionModal modal)
            {
                string newName = modal.ConfirmText.Trim();
                try
                {
                    var targetData = await _targetRepository.GetByDiscordIdAsync(clientId, targetDiscordId);
                    if (targetData == null) throw new UserVisibleException("Target not found.");

                    targetData.Name = newName;
                    var success = await _targetRepository.UpdateAsync(targetData);
                    if (!success) throw new UserVisibleException("Failed to find the target, or the provided name is not unique.");

                    var client = await _apiClientRepository.GetByIdAsync(clientId);

                    var userPermissions = Context.Admin!.Role.RolePermissions.Select(rp => rp.Permission.Key).ToList();
                    var actionMenu = _discordUiService.GetTargetManagementMenuBuilder($"target_select_action:{clientId}:{targetDiscordId}", userPermissions);

                    var components = _discordUiService.CreateTargetOverviewContainer(client!.Name, targetData, cb =>
                    {
                        cb.WithActionRow(row => row.AddComponent(actionMenu));
                    });

                    await ((IModalInteraction)Context.Interaction).UpdateAsync(msg => msg.Components = components);
                    await FollowupAsync($"Target successfully renamed to `{newName}`.", ephemeral: true);
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateException)
                {
                    throw new UserVisibleException($"Failed to rename target. The name `{newName}` is already used by another target in this client.");
                }
            }

            [RequirePermission("api.knownTargets.write")]
            [ComponentInteraction("target_select_crosspost:*:*", ignoreGroupNames: true)]
            public async Task HandleCrosspostSelectAsync(long clientId, ulong targetDiscordId, string[] selectedValues)
            {
                bool newState = bool.Parse(selectedValues[0]);

                var targetData = await _targetRepository.GetByDiscordIdAsync(clientId, targetDiscordId);
                if (targetData == null) throw new UserVisibleException("Target not found.");

                targetData.AutoCrosspost = newState;
                var success = await _targetRepository.UpdateAsync(targetData);
                if (!success) throw new UserVisibleException("Failed to locate the target in the database.");

                var client = await _apiClientRepository.GetByIdAsync(clientId);
                var userPermissions = Context.Admin!.Role.RolePermissions.Select(rp => rp.Permission.Key).ToList();
                var actionMenu = _discordUiService.GetTargetManagementMenuBuilder($"target_select_action:{clientId}:{targetDiscordId}", userPermissions);

                var components = _discordUiService.CreateTargetOverviewContainer(client!.Name, targetData, cb =>
                {
                    cb.WithActionRow(row => row.AddComponent(actionMenu));
                });

                await ((IComponentInteraction)Context.Interaction).UpdateAsync(msg => msg.Components = components);
                await FollowupAsync($"Auto-Publish mode has been updated to **{newState}**.", ephemeral: true);
            }

            [RequirePermission("api.knownTargets.write")]
            [ComponentInteraction("target_btn_sync_confirm:*:*", ignoreGroupNames: true)]
            public async Task HandleSyncConfirmAsync(long clientId, ulong targetDiscordId)
            {
                await DeferAsync(ephemeral: true);

                var target = await _targetRepository.GetByDiscordIdAsync(clientId, targetDiscordId);
                if (target == null) throw new UserVisibleException("Target not found.");

                IChannel? resolvedChannel = null;
                IUser? resolvedUser = null;

                if (target.ChannelType == TextChannelType.DirectMessage)
                    resolvedUser = (Context.Client.GetUser(targetDiscordId) as IUser) ?? await Context.Client.Rest.GetUserAsync(targetDiscordId);
                else
                    resolvedChannel = (Context.Client.GetChannel(targetDiscordId) as IChannel) ?? await Context.Client.Rest.GetChannelAsync(targetDiscordId);

                var result = await _syncService.VerifyAndUpdateTargetAsync(target, resolvedChannel, resolvedUser);
                if (result is null) throw new UserVisibleException("Failed to synchronize target. It violates the allowed channel types and was automatically removed.");

                var client = await _apiClientRepository.GetByIdAsync(clientId);
                var userPermissions = Context.Admin!.Role.RolePermissions.Select(rp => rp.Permission.Key).ToList();
                var actionMenu = _discordUiService.GetTargetManagementMenuBuilder($"target_select_action:{clientId}:{targetDiscordId}", userPermissions);

                var components = _discordUiService.CreateTargetOverviewContainer(client!.Name, result, cb =>
                {
                    cb.WithActionRow(row => row.AddComponent(actionMenu));
                });

                await ((IComponentInteraction)Context.Interaction).ModifyOriginalResponseAsync(msg => msg.Components = components);
                await FollowupAsync("Synchronization complete.", ephemeral: true);
            }

            [RequirePermission("api.knownTargets.write")]
            [ComponentInteraction("target_btn_remove_confirm:*:*", ignoreGroupNames: true)]
            public async Task HandleRemoveConfirmAsync(long clientId, ulong targetDiscordId)
            {
                var target = await _targetRepository.GetByDiscordIdAsync(clientId, targetDiscordId);

                if (target is null) throw new UserVisibleException("Target not found.");


                IUser targetUser = null!;
                if (target.ChannelType is TextChannelType.DirectMessage)
                {
                    targetUser = await Context.Client.Rest.GetUserAsync(target.TargetId);
                }

                var success = await _targetRepository.DeleteByIdAsync(clientId, target.Id);
                if (!success) throw new UserVisibleException("Failed to remove target. It may have already been deleted.");

                if (targetUser != null)
                {
                    var userNotification = _discordUiService.CreateStandardContainer(
                        header: "Authorization to receive notifications revoked",
                        body: $"""
                        Hello {targetUser.Mention},

                        Your access as a notification beneficiary for the API client `{target.IntegrationClient.Name}` has been revoked.
                        This means that this client can no longer send you direct messages through the bot.
                        
                        If you believe this was a mistake or have any questions, please contact {Context.User.Mention} for more information.
                        """,
                        accentColor: AppColors.Error);
                    try
                    {
                        await targetUser.SendMessageAsync(components: userNotification);
                    }
                    catch { }
                }

                var client = await _apiClientRepository.GetByIdAsync(clientId);
                var clientName = client?.Name ?? "Unknown";

                var deletedComponents = _discordUiService.CreateStandardContainer(
                    header: "Target Removed",
                    body: $"The target has been permanently removed from client `{clientName}`.",
                    accentColor: Color.Red);

                await ((IComponentInteraction)Context.Interaction).UpdateAsync(msg => msg.Components = deletedComponents);
            }
        }
    }

    [Group("system", "System-level commands and interactions")]
    public class SystemCommandsGroup : InteractionModuleBase<AppInteractionContext>
    {
        [Group("administration", "Commands for managing system administration")]
        public class AdministrationCommandsGroup : InteractionModuleBase<AppInteractionContext>
        {
            private readonly ILogger<AdministrationCommandsGroup> _logger;
            private readonly ISystemAdministratorRepository _adminRepository;
            private readonly IDiscordUiService _discordUiService;
            private readonly IPaginationService _paginationService;
            private readonly IDiscordEmoteService _emoteCache;
            private readonly IDbContextFactory<ApiSecurityDbContext> _dbFactory;

            public AdministrationCommandsGroup(
                ILogger<AdministrationCommandsGroup> logger,
                ISystemAdministratorRepository adminRepository,
                IDiscordUiService discordUiService,
                IPaginationService paginationService,
                IDiscordEmoteService emoteCache,
                IDbContextFactory<ApiSecurityDbContext> dbFactory)
            {
                _logger = logger;
                _adminRepository = adminRepository;
                _discordUiService = discordUiService;
                _paginationService = paginationService;
                _emoteCache = emoteCache;
                _dbFactory = dbFactory;
            }

            [RequirePermission("system.admins.write")]
            [SlashCommand("create-administrator", "Registers a new system administrator.")]
            public async Task CreateAdministratorAsync(
                [Summary("user", "Select the user to promote")] IUser user,
                [Summary("role_id", "ID of the role from the database (e.g. 2 for Admin, 3 for Moderator)")] int roleId)
            {
                await DeferAsync(ephemeral: true);

                await using var db = await _dbFactory.CreateDbContextAsync();
                var selectedRole = await db.SystemRoles.FindAsync(roleId);
                if (selectedRole == null) throw new UserVisibleException("The specified role ID does not exist in the system.");

                if (Context.Admin!.Role.HierarchyWeight <= selectedRole.HierarchyWeight)
                {
                    throw new UserVisibleException("You can only assign roles that are strictly lower than your own hierarchy weight.");
                }

                if (user.Id == Context.User.Id)
                {
                    throw new UserVisibleException("You cannot manage your own administrative status.");
                }

                if (user.IsBot)
                {
                    throw new UserVisibleException("Bots cannot be registered as system administrators.");
                }

                var existingAdmin = await _adminRepository.GetByDiscordIdAsync(user.Id);
                if (existingAdmin != null)
                {
                    throw new UserVisibleException($"User <@{user.Id}> is already registered in the system.");
                }

                var newAdmin = new SystemAdministrators
                {
                    DiscordUserId = user.Id,
                    CreatedById = Context.Admin!.Id,
                    RoleId = roleId,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                };

                var success = await _adminRepository.AddAsync(newAdmin);
                if (!success) throw new UserVisibleException("An internal database error occurred while creating the administrator.");

                var container = _discordUiService.CreateStandardContainer(
                    header: "Administrator Created",
                    body: $"User <@{user.Id}> has been successfully granted access.\n-# Role: {selectedRole.Name}",
                    accentColor: AppColors.Success);

                var welcomeMessageContainer = _discordUiService.CreateStandardContainer(
                    header: "Welcome to the Administration Team!",
                    body: $"""
                    Hello <@{user.Id}>,
                    
                    You have been added as a system user with the `{selectedRole.Name}` role.
                    Please familiarize yourself with the available commands and use your permissions responsibly.
                    If you have any questions, feel free to reach out to higher-tier administrators.
                    """,
                    accentColor: AppColors.Success);

                try { await user.SendMessageAsync(components: welcomeMessageContainer); } catch { }

                await FollowupAsync(components: container, ephemeral: true, flags: MessageFlags.ComponentsV2);
                _logger.LogInformation("Admin {CreatorId} created new user {NewUserId} with Role ID {Role}", Context.User.Id, user.Id, roleId);
            }

            [RequirePermission("system.admins.read")]
            [SlashCommand("list", "Displays a paginated list of all system administrators.")]
            public async Task ListAdministratorsAsync()
            {
                await DeferAsync(ephemeral: true);

                var dbAdmins = await _adminRepository.GetAllAsync();

                var adminDataList = new List<(SystemAdministrators Entity, IUser? DiscordUser, string Username)>();

                foreach (var admin in dbAdmins)
                {
                    IUser? discordUser = (Context.Client.GetUser(admin.DiscordUserId) as IUser) ?? await Context.Client.Rest.GetUserAsync(admin.DiscordUserId);
                    string username = discordUser?.Username ?? "Unknown User";
                    adminDataList.Add((admin, discordUser, username));
                }

                var sortedAdmins = adminDataList
                    .OrderByDescending(a => a.Entity.Role.HierarchyWeight)
                    .ThenBy(a => a.Username)
                    .ToList();

                var items = new List<string>();

                foreach (var item in sortedAdmins)
                {
                    string usernameDisplay = item.DiscordUser != null ? $"**{item.DiscordUser.Username}**" : $"*Unknown User*";
                    IEmote statusIcon = item.Entity.IsActive ? _emoteCache.GetEmote(IsActive.True.GetDiscordEmote())! : _emoteCache.GetEmote(IsActive.False.GetDiscordEmote())!;
                    string discordCreatedAtTimestamp = $"<t:{((DateTimeOffset)item.Entity.CreatedAtUtc).ToUnixTimeSeconds()}:F>";
                    string discordUpdatedAtTimestamp = item.Entity.UpdatedAtUtc.HasValue ? $"<t:{((DateTimeOffset)item.Entity.UpdatedAtUtc.Value).ToUnixTimeSeconds()}:F>" : "`N/A`";

                    var bodyText = $"""
                        {usernameDisplay} (`{item.Entity.DiscordUserId}`)
                        ├ **Role:** `{item.Entity.Role.Name}`
                        ├ **Status:** {statusIcon} {(item.Entity.IsActive ? "Active" : "Disabled")}
                        ├ **Protected:** {(item.Entity.IsSystemManaged ? "Yes" : "No")}
                        ├ **CreatedAt:** {discordCreatedAtTimestamp}
                        └ **UpdatedAt:** {discordUpdatedAtTimestamp}
                        """;
                    items.Add(bodyText);
                }

                string sessionId = _paginationService.CreatePaginationSession(
                    header: $"System Administrators\n-# Total registered: {dbAdmins.Count}",
                    items: items,
                    charsPerPage: 1200,
                    separator: "\n\n"
                );

                var sessionData = _paginationService.GetSessionData(sessionId);
                if (sessionData == null || sessionData.Pages.Count == 0) throw new UserVisibleException("Failed to generate administrator list.");

                var listComponents = _discordUiService.CreatePaginatedContainer(
                    header: sessionData.Header,
                    pageText: sessionData.Pages[0],
                    currentPage: 1,
                    totalPages: sessionData.Pages.Count,
                    sessionId: sessionId,
                    customActionBtn: sessionData.CustomButton
                );

                await FollowupAsync(components: listComponents, ephemeral: true, flags: MessageFlags.ComponentsV2);
            }

            [RequirePermission("system.admins.read")]
            [SlashCommand("manage-administrator", "Opens the management panel for an administrator.")]
            public async Task ManageAdministratorAsync([Summary("user", "Select the administrator to manage")] IUser targetUser)
            {
                var targetAdmin = await _adminRepository.GetByDiscordIdAsync(targetUser.Id);
                if (targetAdmin == null) throw new UserVisibleException($"User <@{targetUser.Id}> is not an administrator.");

                var actionMenu = _discordUiService.GetAdminActionMenuBuilder($"admin_select_action:{targetAdmin.DiscordUserId}", targetAdmin, Context.Admin!);

                var components = _discordUiService.CreateAdminOverviewContainer(targetAdmin, targetUser, cb =>
                {
                    cb.WithActionRow(row => row.AddComponent(actionMenu));
                });

                await RespondAsync(components: components, ephemeral: true, flags: MessageFlags.ComponentsV2);
            }

            [RequirePermission("system.admins.read")]
            [ComponentInteraction("admin_select_action:*", ignoreGroupNames: true)]
            public async Task HandleAdminActionSelectAsync(ulong targetDiscordId, string[] selectedValues)
            {
                var action = Enum.Parse<BotAdminAction>(selectedValues[0]);
                var targetAdmin = await _adminRepository.GetByDiscordIdAsync(targetDiscordId);
                if (targetAdmin == null) throw new UserVisibleException("Administrator not found.");

                if (Context.User.Id == targetDiscordId)
                    throw new UserVisibleException("You cannot modify your own administrative status.");

                if (Context.Admin!.Role.HierarchyWeight <= targetAdmin.Role.HierarchyWeight)
                    throw new UserVisibleException("You can only manage users with a hierarchy strictly lower than your own.");

                IUser? targetDiscordUser = (Context.Client.GetUser(targetDiscordId) as IUser) ?? await Context.Client.Rest.GetUserAsync(targetDiscordId);
                if (targetDiscordUser == null) throw new UserVisibleException("Could not fetch user from Discord API.");

                var undoEmote = _emoteCache.GetEmote("UI_ICON_UNDO");

                switch (action)
                {
                    case BotAdminAction.ChangeUserRole:
                        {
                            await using var db = await _dbFactory.CreateDbContextAsync();
                            var assignableRoles = await db.SystemRoles
                                .Where(r => r.HierarchyWeight < Context.Admin.Role.HierarchyWeight)
                                .OrderByDescending(r => r.HierarchyWeight)
                                .ToListAsync();

                            if (assignableRoles.Count == 0) throw new UserVisibleException("There are no roles available for you to assign.");

                            var roleComponents = _discordUiService.CreateAdminOverviewContainer(targetAdmin, targetDiscordUser, cb =>
                            {
                                cb.WithActionRow(row => row.AddComponent(_discordUiService.GetSystemRoleMenuBuilder($"admin_set_role:{targetDiscordId}", targetAdmin.RoleId, assignableRoles)));
                                cb.WithActionRow(row => row.AddComponent(new ButtonBuilder().WithCustomId($"admin_btn_cancel:{targetDiscordId}").WithLabel("Return").WithStyle(ButtonStyle.Secondary).WithEmote(undoEmote)));
                            });
                            await ((IComponentInteraction)Context.Interaction).UpdateAsync(msg => msg.Components = roleComponents);
                        }
                        break;

                    case BotAdminAction.EnableOrDisableUser:
                        var statusComponents = _discordUiService.CreateAdminOverviewContainer(targetAdmin, targetDiscordUser, cb =>
                        {
                            cb.WithActionRow(row => row.AddComponent(_discordUiService.GetAdminStatusMenuBuilder($"admin_set_status:{targetDiscordId}", targetAdmin.IsActive)));
                            cb.WithActionRow(row => row.AddComponent(new ButtonBuilder().WithCustomId($"admin_btn_cancel:{targetDiscordId}").WithLabel("Return").WithStyle(ButtonStyle.Secondary).WithEmote(undoEmote)));
                        });
                        await ((IComponentInteraction)Context.Interaction).UpdateAsync(msg => msg.Components = statusComponents);
                        break;
                }
            }

            [RequirePermission("system.admins.write")]
            [ComponentInteraction("admin_set_role:*", ignoreGroupNames: true)]
            public async Task HandleSetRoleAsync(ulong targetDiscordId, string[] selectedValues)
            {
                int newRoleId = int.Parse(selectedValues[0]);
                var targetAdmin = await _adminRepository.GetByDiscordIdAsync(targetDiscordId);
                if (targetAdmin == null) throw new UserVisibleException("Administrator not found.");

                if (Context.User.Id == targetDiscordId)
                    throw new UserVisibleException("Unauthorized action.");

                if (Context.Admin!.Role.HierarchyWeight <= targetAdmin.Role.HierarchyWeight)
                    throw new UserVisibleException("You can only change permissions of users with a hierarchy strictly lower than your own.");

                await using var db = await _dbFactory.CreateDbContextAsync();
                var newRole = await db.SystemRoles.FindAsync(newRoleId);
                if (newRole == null) throw new UserVisibleException("The selected role does not exist.");

                if (Context.Admin!.Role.HierarchyWeight <= newRole.HierarchyWeight)
                    throw new UserVisibleException("You cannot assign a role with a hierarchy weight equal to or higher than your own.");

                targetAdmin.RoleId = newRoleId;
                var success = await _adminRepository.UpdateAsync(targetAdmin);
                if (!success) throw new UserVisibleException("Database error occurred while updating the role.");

                if (success)
                {
                    var messageContainer = _discordUiService.CreateStandardContainer(
                        header: "Role Updated",
                        body: $"Your system role has been changed to `{newRole.Name}` by {Context.User.Mention}.",
                        accentColor: AppColors.Warning);

                    try
                    {
                        var messageTarget = await Context.Client.Rest.GetUserAsync(targetDiscordId);
                        if (messageTarget != null)
                        {
                            await messageTarget.SendMessageAsync(components: messageContainer);
                        }
                    }
                    catch { }
                }

                IUser? targetDiscordUser = (Context.Client.GetUser(targetDiscordId) as IUser) ?? await Context.Client.Rest.GetUserAsync(targetDiscordId);
                if (targetDiscordUser == null) throw new UserVisibleException("Could not fetch user from Discord API.");

                var refreshedTargetAdmin = await _adminRepository.GetByDiscordIdAsync(targetDiscordId);

                var actionMenu = _discordUiService.GetAdminActionMenuBuilder($"admin_select_action:{targetDiscordId}", refreshedTargetAdmin!, Context.Admin!);
                var components = _discordUiService.CreateAdminOverviewContainer(refreshedTargetAdmin!, targetDiscordUser, cb =>
                {
                    cb.WithActionRow(row => row.AddComponent(actionMenu));
                });

                await ((IComponentInteraction)Context.Interaction).UpdateAsync(msg => msg.Components = components);
                await FollowupAsync($"Role updated to **{newRole.Name}**.", ephemeral: true);
            }

            [RequirePermission("system.admins.write")]
            [ComponentInteraction("admin_set_status:*", ignoreGroupNames: true)]
            public async Task HandleSetStatusAsync(ulong targetDiscordId, string[] selectedValues)
            {
                bool newState = bool.Parse(selectedValues[0]);
                var targetAdmin = await _adminRepository.GetByDiscordIdAsync(targetDiscordId);
                if (targetAdmin == null) throw new UserVisibleException("Administrator not found.");

                if (Context.User.Id == targetDiscordId)
                    throw new UserVisibleException("Unauthorized action.");

                if (Context.Admin!.Role.HierarchyWeight <= targetAdmin.Role.HierarchyWeight)
                    throw new UserVisibleException("You can only change the status of users with a hierarchy strictly lower than your own.");

                targetAdmin.IsActive = newState;
                var success = await _adminRepository.UpdateAsync(targetAdmin);
                if (!success) throw new UserVisibleException("Database error occurred while updating the status.");

                IUser? targetDiscordUser = (Context.Client.GetUser(targetDiscordId) as IUser) ?? await Context.Client.Rest.GetUserAsync(targetDiscordId);
                if (targetDiscordUser == null) throw new UserVisibleException("Could not fetch user from Discord API.");

                var actionMenu = _discordUiService.GetAdminActionMenuBuilder($"admin_select_action:{targetDiscordId}", targetAdmin, Context.Admin!);
                var components = _discordUiService.CreateAdminOverviewContainer(targetAdmin, targetDiscordUser, cb =>
                {
                    cb.WithActionRow(row => row.AddComponent(actionMenu));
                });

                if (newState is true)
                {
                    var enableMessageContainer = _discordUiService.CreateStandardContainer(
                        header: "Account Enabled",
                        body: $"Your account has been re-enabled by {Context.User.Mention}. You now have access to the system again.",
                        accentColor: AppColors.Success);

                    try { await targetDiscordUser.SendMessageAsync(components: enableMessageContainer); } catch { }
                }
                else
                {
                    var disableMessageContainer = _discordUiService.CreateStandardContainer(
                        header: "Account Suspended",
                        body: $"Your account has been suspended by {Context.User.Mention}. You no longer have access to the system.",
                        accentColor: AppColors.Error);

                    try { await targetDiscordUser.SendMessageAsync(components: disableMessageContainer); } catch { }
                }

                await ((IComponentInteraction)Context.Interaction).UpdateAsync(msg => msg.Components = components);
                await FollowupAsync($"Status updated to **{(newState ? "ACTIVE" : "DISABLED")}**.", ephemeral: true);
            }

            [RequirePermission("system.admins.read")]
            [ComponentInteraction("admin_btn_cancel:*", ignoreGroupNames: true)]
            public async Task HandleAdminCancelAsync(ulong targetDiscordId)
            {
                var targetAdmin = await _adminRepository.GetByDiscordIdAsync(targetDiscordId);
                if (targetAdmin == null) throw new UserVisibleException("Administrator not found.");

                IUser? targetDiscordUser = (Context.Client.GetUser(targetDiscordId) as IUser) ?? await Context.Client.Rest.GetUserAsync(targetDiscordId);
                if (targetDiscordUser == null) throw new UserVisibleException("Could not fetch user from Discord API.");

                var actionMenu = _discordUiService.GetAdminActionMenuBuilder($"admin_select_action:{targetDiscordId}", targetAdmin, Context.Admin!);

                var components = _discordUiService.CreateAdminOverviewContainer(targetAdmin, targetDiscordUser, cb =>
                {
                    cb.WithActionRow(row => row.AddComponent(actionMenu));
                });

                await ((IComponentInteraction)Context.Interaction).UpdateAsync(msg => msg.Components = components);
            }
        }
    }

    public class ZabbixDirectMessageComponents : InteractionModuleBase<AppInteractionContext>
    {
        private readonly ILogger<ClientCommandsGroup> _logger;
        private readonly IApiSecurityStore _apiSecurityStore;
        private readonly IIntegrationClientRepository _apiClientRepository;
        private readonly IKnownDeliveryTargetRepository _targetRepository;
        private readonly IDiscordUiService _discordUiService;
        private readonly IPaginationService _paginationService;
        private readonly IDiscordEmoteService _emoteCache;
        private readonly ZabbixService _zabbixService;

        public ZabbixDirectMessageComponents(
            ILogger<ClientCommandsGroup> logger,
            IApiSecurityStore apiSecurityStore,
            IIntegrationClientRepository apiClientRepository,
            IKnownDeliveryTargetRepository targetRepository,
            IDiscordUiService discordUiService,
            IPaginationService paginationService,
            IDiscordEmoteService emoteCache,
            ZabbixService zabbixService)
        {
            _logger = logger;
            _apiSecurityStore = apiSecurityStore;
            _apiClientRepository = apiClientRepository;
            _targetRepository = targetRepository;
            _discordUiService = discordUiService;
            _paginationService = paginationService;
            _emoteCache = emoteCache;
            _zabbixService = zabbixService;
        }

        [ComponentInteraction("btn_manage:*:*", ignoreGroupNames: true)]
        public async Task HandleManageButton([RequireActiveApiClient] long apiId, string eventId)
        {
            await DeferAsync(ephemeral: true);

            var zabbixEvent = await _zabbixService.GetEventDetailsAsync(apiId, eventId);
            if (zabbixEvent == null) throw new UserVisibleException("Failed to retrieve event details from the Zabbix server.");

            bool currentAckState = zabbixEvent.Acknowledged == 1;
            int currentSev = zabbixEvent.Severity;

            var ackMenu = _discordUiService.GetZabbixAckMenuBuilder($"zabbix_select_ack:{apiId}:{eventId}", currentAckState);
            var sevMenu = _discordUiService.GetZabbixSeverityMenuBuilder($"zabbix_select_sev:{apiId}:{eventId}", currentSev);
            var commentBtn = new ButtonBuilder()
                .WithCustomId($"zabbix_btn_comment:{apiId}:{eventId}")
                .WithLabel("Add Comment")
                .WithStyle(ButtonStyle.Primary)
                .WithEmote(new Emoji("💬"));

            var panel = _discordUiService.CreateZabbixManagementPanel(eventId, ackMenu, sevMenu, commentBtn);

            await FollowupAsync(components: panel, ephemeral: true, flags: MessageFlags.ComponentsV2);
        }

        [ComponentInteraction("zabbix_select_ack:*:*", ignoreGroupNames: true)]
        public async Task HandleZabbixAckSelect(long apiId, string eventId, string[] selectedValues)
        {
            await DeferAsync(ephemeral: true);
            bool newAckState = bool.Parse(selectedValues[0]);

            var zabbixEvent = await _zabbixService.GetEventDetailsAsync(apiId, eventId);
            if (zabbixEvent == null) throw new UserVisibleException("Event data missing.");

            bool success = await _zabbixService.AcknowledgeEventAsync(apiId, eventId, null, newAckState, false, zabbixEvent.Severity);
            if (!success) throw new UserVisibleException("Zabbix API rejected the request.");

            var ackMenu = _discordUiService.GetZabbixAckMenuBuilder($"zabbix_select_ack:{apiId}:{eventId}", newAckState);
            var sevMenu = _discordUiService.GetZabbixSeverityMenuBuilder($"zabbix_select_sev:{apiId}:{eventId}", zabbixEvent.Severity);
            var commentBtn = new ButtonBuilder().WithCustomId($"zabbix_btn_comment:{apiId}:{eventId}").WithLabel("Add Comment").WithStyle(ButtonStyle.Primary).WithEmote(new Emoji("💬"));
            var panel = _discordUiService.CreateZabbixManagementPanel(eventId, ackMenu, sevMenu, commentBtn);

            await ((IComponentInteraction)Context.Interaction).ModifyOriginalResponseAsync(msg => msg.Components = panel);
        }

        [ComponentInteraction("zabbix_select_sev:*:*", ignoreGroupNames: true)]
        public async Task HandleZabbixSevSelect(long apiId, string eventId, string[] selectedValues)
        {
            await DeferAsync(ephemeral: true);
            int newSevValue = int.Parse(selectedValues[0]);

            var zabbixEvent = await _zabbixService.GetEventDetailsAsync(apiId, eventId);
            if (zabbixEvent == null) throw new UserVisibleException("The event does not exist on the server.");

            bool currentAckState = zabbixEvent.Acknowledged == 1;

            bool success = await _zabbixService.AcknowledgeEventAsync(apiId, eventId, null, currentAckState, false, newSevValue);
            if (!success) throw new UserVisibleException("Zabbix API rejected the request.");

            var ackMenu = _discordUiService.GetZabbixAckMenuBuilder($"zabbix_select_ack:{apiId}:{eventId}", currentAckState);
            var sevMenu = _discordUiService.GetZabbixSeverityMenuBuilder($"zabbix_select_sev:{apiId}:{eventId}", newSevValue);
            var commentBtn = new ButtonBuilder().WithCustomId($"zabbix_btn_comment:{apiId}:{eventId}").WithLabel("Add Comment").WithStyle(ButtonStyle.Primary).WithEmote(new Emoji("💬"));
            var panel = _discordUiService.CreateZabbixManagementPanel(eventId, ackMenu, sevMenu, commentBtn);

            await ((IComponentInteraction)Context.Interaction).ModifyOriginalResponseAsync(msg => msg.Components = panel);
        }

        [ComponentInteraction("zabbix_btn_comment:*:*", ignoreGroupNames: true)]
        public async Task HandleZabbixCommentBtn(long apiId, string eventId)
        {
            var modal = _discordUiService.CreateZabbixCommentModal($"zabbix_modal_comment:{apiId}:{eventId}");
            await RespondWithModalAsync(modal);
        }

        [ModalInteraction("zabbix_modal_comment:*:*", ignoreGroupNames: true)]
        public async Task HandleActionModal([RequireActiveApiClient] long apiId, string eventId, ZabbixCommentModal modalData)
        {
            await DeferAsync(ephemeral: true);

            var comment = modalData.Comment;

            var zabbixEvent = await _zabbixService.GetEventDetailsAsync(apiId, eventId);
            bool currentAckState = zabbixEvent?.Acknowledged == 1;
            int currentSev = zabbixEvent?.Severity ?? 0;

            bool success = await _zabbixService.AcknowledgeEventAsync(apiId, eventId, comment, currentAckState, false, currentSev);

            if (success)
            {
                await FollowupAsync($"Comment added to event `{eventId}`.", ephemeral: true);
                _logger.LogInformation("Successfully added comment to Zabbix event {EventId}", eventId);
            }
            else
            {
                throw new UserVisibleException("Zabbix API rejected the request.");
            }
        }
    }
}
