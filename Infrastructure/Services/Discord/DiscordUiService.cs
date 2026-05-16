using Application.Common.Constants;
using Application.Common.Zabbix;
using Application.Services.Discord;
using Discord;
using Discord.WebSocket;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Extensions;
using System.Reflection;
using System.Text;

namespace Infrastructure.Services.Discord
{
    public class DiscordUiService(DiscordSocketClient client, IDiscordEmoteService emoteCache) : IDiscordUiService
    {
        private readonly DiscordSocketClient _client = client;
        private readonly IDiscordEmoteService _emoteCache = emoteCache;

        private string BuildFooterText()
        {
            var githubIcon = _emoteCache.GetEmote("UI_ICON_GITHUB_WHITE");
            string appAuthor = $"{githubIcon}**[Sh00ter0](https://github.com/Sh00ter0)**";
            Version? version = Assembly.GetEntryAssembly()?.GetName().Version;
            int major = version?.Major ?? 0;
            int minor = version?.Minor ?? 1;
            int build = version?.Build ?? 0;

            var sb = new StringBuilder();
            sb.Append($"""
                -# [Zabbix-Discord Bridge](https://github.com/Sh00ter0/zdb)
                -# Copyright (c) 2026 — {appAuthor}
                -# `v{major}.{minor}.{build}`
                """);
            return sb.ToString();
        }

        public MessageComponent CreateStandardContainer(string header, string body, Color? accentColor = null, string? footerNote = null, Action<ContainerBuilder>? appendComponents = null)
        {
            var color = accentColor ?? AppColors.Info;
            string? avatarUrl = _client.CurrentUser?.GetDisplayAvatarUrl() ?? _client.CurrentUser?.GetDefaultAvatarUrl();

            var containerBuilder = new ContainerBuilder().WithAccentColor(color);

            if (!string.IsNullOrEmpty(avatarUrl))
                containerBuilder.WithSection([new TextDisplayBuilder($"‎‎‎\n### {header}")], new ThumbnailBuilder(avatarUrl));
            else
                containerBuilder.WithTextDisplay($"## {header}");

            containerBuilder.WithSeparator(spacing: SeparatorSpacingSize.Large).WithTextDisplay(body);

            // <--- NOWE: Jeśli przekazaliśmy komponenty UI (przyciski/selecty), doklejamy je --->
            if (appendComponents != null)
            {
                containerBuilder.WithSeparator(spacing: SeparatorSpacingSize.Small, false);
                appendComponents.Invoke(containerBuilder);
            }

            containerBuilder.WithSeparator(spacing: SeparatorSpacingSize.Large).WithTextDisplay(BuildFooterText());
            return new ComponentBuilderV2().WithContainer(containerBuilder).Build();
        }

        public Modal CreateSingleInputModal(string customId, string title, string inputLabel, string placeholder, int maxLength = 100, bool isParagraph = false)
        {
            return new ModalBuilder()
                .WithTitle(title)
                .WithCustomId(customId)
                .AddTextInput(label: inputLabel, customId: "input1", style: isParagraph ? TextInputStyle.Paragraph : TextInputStyle.Short, placeholder: placeholder, required: true, maxLength: maxLength)
                .Build();
        }

        public Modal CreateDualInputModal(string customId, string title, string label1, string label2, string placeholder1, string placeholder2)
        {
            return new ModalBuilder()
                .WithTitle(title)
                .WithCustomId(customId)
                .AddTextInput(label: label1, customId: "input1", style: TextInputStyle.Short, placeholder: placeholder1, required: true)
                .AddTextInput(label: label2, customId: "input2", style: TextInputStyle.Short, placeholder: placeholder2, required: true)
                .Build();
        }

        public MessageComponent CreatePaginatedContainer(string header, string pageText, int currentPage, int totalPages, string sessionId, Color? accentColor = null, ButtonBuilder? customActionBtn = null)
        {
            var color = accentColor ?? AppColors.Info;
            string? avatarUrl = _client.CurrentUser?.GetDisplayAvatarUrl() ?? _client.CurrentUser?.GetDefaultAvatarUrl();
            var containerBuilder = new ContainerBuilder().WithAccentColor(color);

            if (!string.IsNullOrEmpty(avatarUrl))
                containerBuilder.WithSection([new TextDisplayBuilder($"‎‎‎\n### {header}")], new ThumbnailBuilder(avatarUrl));
            else
                containerBuilder.WithTextDisplay($"## {header}");

            containerBuilder.WithSeparator(spacing: SeparatorSpacingSize.Large).WithTextDisplay(pageText).WithSeparator(spacing: SeparatorSpacingSize.Large).WithTextDisplay(BuildFooterText());

            if (totalPages > 1 || customActionBtn != null)
            {
                containerBuilder.WithActionRow(row =>
                {
                    if (totalPages > 1)
                    {
                        row.AddComponent(new ButtonBuilder().WithCustomId($"nav:{sessionId}:1:first").WithEmote(new Emoji("⏪")).WithStyle(ButtonStyle.Secondary).WithDisabled(currentPage == 1));
                        row.AddComponent(new ButtonBuilder().WithCustomId($"nav:{sessionId}:{currentPage - 1}:prev").WithEmote(new Emoji("⬅️")).WithStyle(ButtonStyle.Secondary).WithDisabled(currentPage == 1));
                        row.AddComponent(new ButtonBuilder().WithCustomId($"nav:{sessionId}:{currentPage + 1}:next").WithEmote(new Emoji("➡️")).WithStyle(ButtonStyle.Secondary).WithDisabled(currentPage == totalPages));
                        row.AddComponent(new ButtonBuilder().WithCustomId($"nav:{sessionId}:{totalPages}:last").WithEmote(new Emoji("⏩")).WithStyle(ButtonStyle.Secondary).WithDisabled(currentPage == totalPages));
                    }
                    if (customActionBtn != null)
                        row.AddComponent(customActionBtn);
                });
            }
            return new ComponentBuilderV2().WithContainer(containerBuilder).Build();
        }

        public MessageComponent CreateZabbixAlertContainer(ZabbixPayload payload, bool isDM, long apiClientId)
        {
            Color containerColor = payload.EventSource switch
            {
                1 or 2 => AppColors.Info,
                _ => payload.EventValue == 0 ? AppColors.Success : payload.Severity switch
                {
                    1 => AppColors.SeverityInformation,
                    2 => AppColors.SeverityWarning,
                    3 => AppColors.SeverityAverage,
                    4 => AppColors.SeverityHigh,
                    5 => AppColors.SeverityDisaster,
                    _ => AppColors.SeverityNotClassified
                }
            };

            string safeMessage = payload.Message.Length >= 3900 ? payload.Message[..3900] + "…" : payload.Message;
            string? avatarUrl = _client.CurrentUser?.GetDisplayAvatarUrl() ?? _client.CurrentUser?.GetDefaultAvatarUrl();

            var containerBuilder = new ContainerBuilder().WithAccentColor(containerColor);

            if (!string.IsNullOrEmpty(avatarUrl)) containerBuilder.WithSection([new TextDisplayBuilder($"‎‎‎\n### {payload.Subject}")], new ThumbnailBuilder(avatarUrl));
            else containerBuilder.WithTextDisplay($"## {payload.Subject}");

            containerBuilder.WithSeparator(spacing: SeparatorSpacingSize.Large).WithTextDisplay(safeMessage).WithSeparator(spacing: SeparatorSpacingSize.Large).WithTextDisplay(BuildFooterText());

            if (isDM && payload.ControlMenu == 1 && payload.EventValue != 0 && payload.EventSource != 1 && payload.EventSource != 2)
            {
                var pulseIcon = _emoteCache.GetEmote("UI_ICON_PULSE");
                // Using Manage constant
                containerBuilder.WithActionRow(row => row.AddComponent(new ButtonBuilder().WithCustomId($"zabbix_action:{apiClientId}:{payload.EventId}:{DiscordComponentActions.Manage}").WithLabel("Take action").WithStyle(ButtonStyle.Primary).WithEmote(pulseIcon)));
            }
            return new ComponentBuilderV2().WithContainer(containerBuilder).Build();
        }

        public SelectMenuBuilder GetZabbixAckMenuBuilder(string customId, bool currentState)
        {
            return new SelectMenuBuilder().WithCustomId(customId).WithPlaceholder("Acknowledgment Status...")
                .AddOption("Acknowledged", "true", "Mark event as acknowledged", isDefault: currentState, emote: _emoteCache.GetEmote("UI_ICON_CHECK"))
                .AddOption("Not Acknowledged", "false", "Leave event unacknowledged", isDefault: !currentState, emote: _emoteCache.GetEmote("UI_ICON_X"));
        }

        public SelectMenuBuilder GetZabbixSeverityMenuBuilder(string customId, int currentSeverity)
        {
            var sevMenu = new SelectMenuBuilder().WithCustomId(customId).WithPlaceholder("Update Severity...");
            foreach (ZabbixSeverity severity in Enum.GetValues(typeof(ZabbixSeverity)))
            {
                var optionInfo = severity.GetDiscordOptionInfo();
                if (optionInfo != null)
                    sevMenu.AddOption(label: optionInfo.Label, value: ((int)severity).ToString(), description: optionInfo.Description, emote: _emoteCache.GetEmote(optionInfo.Emote), isDefault: (int)severity == currentSeverity);
            }
            return sevMenu;
        }

        public MessageComponent CreateZabbixManagementPanel(string eventId, SelectMenuBuilder ackMenu, SelectMenuBuilder sevMenu, ButtonBuilder commentBtn)
        {
            var containerBuilder = new ContainerBuilder().WithAccentColor(AppColors.Info).WithTextDisplay($"## Manage Event: `{eventId}`\nChange the status below to update Zabbix in real-time.")
                .WithSeparator(SeparatorSpacingSize.Large).WithActionRow(row => row.AddComponent(ackMenu)).WithActionRow(row => row.AddComponent(sevMenu))
                .WithActionRow(row => row.AddComponent(commentBtn)).WithSeparator(SeparatorSpacingSize.Small).WithTextDisplay(BuildFooterText());
            return new ComponentBuilderV2().WithContainer(containerBuilder).Build();
        }

        public MessageComponent CreateApiClientOverviewContainer(IntegrationClients client, Action<ContainerBuilder>? appendComponents = null)
        {
            var bulbIconON = _emoteCache.GetEmote("UI_ICON_BULB_ON");
            var bulbIconOFF = _emoteCache.GetEmote("UI_ICON_BULB_OFF");
            var discordCreateTimestamp = $"<t:{((DateTimeOffset)client.CreatedAtUtc).ToUnixTimeSeconds()}:F>";
            var discordUpdateTimestamp = client.UpdatedAtUtc != null ? $"<t:{((DateTimeOffset)client.UpdatedAtUtc).ToUnixTimeSeconds()}:F>" : "`N/A`";
            string statusIcon = client.IsActive ? $"{bulbIconON}`Active`" : $"{bulbIconOFF}`Disabled`";
            string zabbixUrl = string.IsNullOrEmpty(client.ZabbixCredential?.ApiUrl) ? "`Not Configured`" : $"`{client.ZabbixCredential.ApiUrl}`";

            var bodyText = $"""
                **Client Name:** `{client.Name}`
                **Status:** {statusIcon}
                **Key Preview:** `{client.KeyPreview}`
                **Zabbix URL:** {zabbixUrl}
                **Created At:** {discordCreateTimestamp}
                **Updated At:** {discordUpdateTimestamp}
                """;

            string? avatarUrl = _client.CurrentUser?.GetDisplayAvatarUrl() ?? _client.CurrentUser?.GetDefaultAvatarUrl();
            var containerBuilder = new ContainerBuilder().WithAccentColor(new Color(AppColors.Info));

            if (!string.IsNullOrEmpty(avatarUrl)) containerBuilder.WithSection([new TextDisplayBuilder($"‎‎‎\n### Manage API Client")], new ThumbnailBuilder(avatarUrl));
            else containerBuilder.WithTextDisplay($"## Manage API Client");

            containerBuilder.WithSeparator(spacing: SeparatorSpacingSize.Large).WithTextDisplay(bodyText);

            if (appendComponents != null)
            {
                containerBuilder.WithSeparator(spacing: SeparatorSpacingSize.Small, false);
                appendComponents.Invoke(containerBuilder);
            }
            containerBuilder.WithSeparator(spacing: SeparatorSpacingSize.Small).WithTextDisplay(BuildFooterText());
            return new ComponentBuilderV2().WithContainer(containerBuilder).Build();
        }

        public SelectMenuBuilder GetApiClientManagementMenuBuilder(string customId, List<string> userPermissions)
        {
            var menuBuilder = new SelectMenuBuilder().WithCustomId(customId).WithPlaceholder("Select a client action to perform...");
            bool isRoot = userPermissions.Contains("root");

            foreach (ApiClientModifyingAction action in Enum.GetValues(typeof(ApiClientModifyingAction)))
            {
                var optionInfo = action.GetDiscordOptionInfo();
                if (optionInfo != null && (isRoot || optionInfo.RequiredPermission == null || userPermissions.Contains(optionInfo.RequiredPermission)))
                {
                    // 100% bezpieczne, silnie typowane mapowanie na klucze naszego routera
                    string routingKey = action switch
                    {
                        ApiClientModifyingAction.ChangeName => "open_rename",
                        ApiClientModifyingAction.EnableOrDisableClient => "open_status",
                        ApiClientModifyingAction.RenewZabbixConnection => "open_zabbix",
                        ApiClientModifyingAction.DisplayRelatedTargets => "open_targets",
                        ApiClientModifyingAction.RenewApiKey => "prompt_renew",
                        ApiClientModifyingAction.Remove => "prompt_delete",
                        _ => action.ToString()
                    };

                    menuBuilder.AddOption(
                        label: optionInfo.Label,
                        value: routingKey, // Wstrzykujemy klucz akcji
                        description: optionInfo.Description,
                        emote: _emoteCache.GetEmote(optionInfo.Emote));
                }
            }

            if (menuBuilder.Options.Count == 0)
                menuBuilder.WithPlaceholder("No actions available").AddOption("Insufficient permissions", "none", "You do not have permission to manage this.").WithDisabled(true);

            return menuBuilder;
        }

        public SelectMenuBuilder GetClientStatusSelectMenuBuilder(string customId, bool currentState)
        {
            return new SelectMenuBuilder().WithCustomId(customId).WithPlaceholder("Select client status...")
                .AddOption("Enabled", DiscordComponentActions.StatusTrue, "Client is active and processing requests", isDefault: currentState, emote: _emoteCache.GetEmote("UI_ICON_BULB_ON"))
                .AddOption("Disabled", DiscordComponentActions.StatusFalse, "Client is inactive and will reject requests", isDefault: !currentState, emote: _emoteCache.GetEmote("UI_ICON_BULB_OFF"));
        }

        public MessageComponent CreateTargetOverviewContainer(string clientName, KnownDeliveryTargets target, Action<ContainerBuilder>? appendComponents = null)
        {
            var discordCreatedAtTimestamp = $"<t:{((DateTimeOffset)target.CreatedAtUtc).ToUnixTimeSeconds()}:F>";
            var discordUpdatedAtTimestamp = target.UpdatedAtUtc != null ? $"<t:{((DateTimeOffset)target.UpdatedAtUtc).ToUnixTimeSeconds()}:F>" : "`N/A`";
            string friendlyChannelType = target.ChannelType.GetDiscordLabel();
            string guildInfo = target.AssociatedGuildId.HasValue ? $"`{target.AssociatedGuildId.Value}`" : "`N/A`";

            var bodyText = $"""
                **Name:** `{target.Name}`
                **Target ID:** `{target.TargetId}`
                **Type:** `{friendlyChannelType}`
                **Associated Guild:** {guildInfo}
                **Auto-Publish:** `{target.AutoCrosspost}`
                **Created At:** {discordCreatedAtTimestamp}
                **Updated At:** {discordUpdatedAtTimestamp}
                """;

            string? avatarUrl = _client.CurrentUser?.GetDisplayAvatarUrl() ?? _client.CurrentUser?.GetDefaultAvatarUrl();
            var containerBuilder = new ContainerBuilder().WithAccentColor(AppColors.Info);

            if (!string.IsNullOrEmpty(avatarUrl)) containerBuilder.WithSection([new TextDisplayBuilder($"‎‎‎\n### Manage Target: {clientName}")], new ThumbnailBuilder(avatarUrl));
            else containerBuilder.WithTextDisplay($"## Manage Target: {clientName}");

            containerBuilder.WithSeparator(spacing: SeparatorSpacingSize.Large).WithTextDisplay(bodyText);

            if (appendComponents != null)
            {
                containerBuilder.WithSeparator(spacing: SeparatorSpacingSize.Small, false);
                appendComponents.Invoke(containerBuilder);
            }
            containerBuilder.WithSeparator(spacing: SeparatorSpacingSize.Large).WithTextDisplay(BuildFooterText());
            return new ComponentBuilderV2().WithContainer(containerBuilder).Build();
        }

        public SelectMenuBuilder GetTargetManagementMenuBuilder(string customId, List<string> userPermissions)
        {
            var menuBuilder = new SelectMenuBuilder().WithCustomId(customId).WithPlaceholder("Select an action to perform...");
            bool isRoot = userPermissions.Contains("root");

            foreach (AllowedTargetModifyingAction action in Enum.GetValues(typeof(AllowedTargetModifyingAction)))
            {
                var optionInfo = action.GetDiscordOptionInfo();
                if (optionInfo != null && (isRoot || optionInfo.RequiredPermission == null || userPermissions.Contains(optionInfo.RequiredPermission)))
                    menuBuilder.AddOption(label: optionInfo.Label, value: action.ToString(), description: optionInfo.Description, emote: _emoteCache.GetEmote(optionInfo.Emote));
            }

            if (menuBuilder.Options.Count == 0)
                menuBuilder.WithPlaceholder("No actions available").AddOption("Insufficient permissions", "none", "You do not have permission to manage this.").WithDisabled(true);

            return menuBuilder;
        }

        public SelectMenuBuilder GetCrosspostSelectMenuBuilder(string customId, bool currentState)
        {
            return new SelectMenuBuilder().WithCustomId(customId).WithPlaceholder("Select auto-publish mode...")
                .AddOption("Enable Auto-Publish", DiscordComponentActions.CrosspostTrue, "Messages will be automatically published", isDefault: currentState, emote: _emoteCache.GetEmote("UI_ICON_CHECK"))
                .AddOption("Disable Auto-Publish", DiscordComponentActions.CrosspostFalse, "Messages will NOT be automatically published", isDefault: !currentState, emote: _emoteCache.GetEmote("UI_ICON_X"));
        }

        public MessageComponent CreateAdminOverviewContainer(SystemAdministrators adminEntity, IUser discordUser, Action<ContainerBuilder>? appendComponents = null)
        {
            string discordCreatedAtTimestamp = $"<t:{((DateTimeOffset)adminEntity.CreatedAtUtc).ToUnixTimeSeconds()}:F>";
            string discordUpdatedAtTimestamp = adminEntity.UpdatedAtUtc != null ? $"<t:{((DateTimeOffset)adminEntity.UpdatedAtUtc.Value).ToUnixTimeSeconds()}:F>" : "`N/A`";
            IEmote? statusIcon = adminEntity.IsActive ? _emoteCache.GetEmote("UI_ICON_BULB_ON") : _emoteCache.GetEmote("UI_ICON_BULB_OFF");

            var bodyText = $"""
                **User:** {discordUser.Mention} (`{discordUser.Id}`)
                **Role:** `{adminEntity.Role.Name}` *(Weight: {adminEntity.Role.HierarchyWeight})*
                **Status:** {statusIcon} {(adminEntity.IsActive ? "`Active`" : "`Disabled`")}
                **System Managed:** {(adminEntity.IsSystemManaged ? "`Yes` (Protected)" : "`No`")}
                **Created At:** {discordCreatedAtTimestamp}
                **Updated At:** {discordUpdatedAtTimestamp}
                """;

            string avatarUrl = discordUser.GetDisplayAvatarUrl() ?? discordUser.GetDefaultAvatarUrl();
            var containerBuilder = new ContainerBuilder().WithAccentColor(AppColors.Info);

            containerBuilder.WithSection([new TextDisplayBuilder($"‎‎‎\n### User Administration")], new ThumbnailBuilder(avatarUrl));
            containerBuilder.WithSeparator(spacing: SeparatorSpacingSize.Large).WithTextDisplay(bodyText);

            if (appendComponents != null)
            {
                containerBuilder.WithSeparator(spacing: SeparatorSpacingSize.Small, false);
                appendComponents.Invoke(containerBuilder);
            }
            containerBuilder.WithSeparator(spacing: SeparatorSpacingSize.Large).WithTextDisplay(BuildFooterText());
            return new ComponentBuilderV2().WithContainer(containerBuilder).Build();
        }

        public SelectMenuBuilder GetAdminActionMenuBuilder(string customId, SystemAdministrators targetAdmin, SystemAdministrators requestingAdmin)
        {
            var menuBuilder = new SelectMenuBuilder().WithCustomId(customId).WithPlaceholder("Select an administrative action...");
            bool isSelf = targetAdmin.DiscordUserId == requestingAdmin.DiscordUserId;

            if (isSelf || requestingAdmin.Role.HierarchyWeight <= targetAdmin.Role.HierarchyWeight || targetAdmin.IsSystemManaged)
            {
                menuBuilder.WithPlaceholder("No actions available").AddOption("Protected / Insufficient permissions", "none", "Account is immutable.").WithDisabled(true);
                return menuBuilder;
            }

            foreach (BotAdminAction action in Enum.GetValues(typeof(BotAdminAction)))
            {
                var optionInfo = action.GetDiscordOptionInfo();
                menuBuilder.AddOption(label: optionInfo?.Label ?? action.ToString(), value: action.ToString(), description: optionInfo?.Description, emote: _emoteCache.GetEmote(optionInfo?.Emote));
            }

            if (menuBuilder.Options.Count == 0)
                menuBuilder.WithPlaceholder("No actions available").AddOption("Insufficient permissions", "none", "You cannot manage this user.").WithDisabled(true);

            return menuBuilder;
        }

        public SelectMenuBuilder GetSystemRoleMenuBuilder(string customId, int currentRoleId, List<SystemRoles> assignableRoles)
        {
            var menuBuilder = new SelectMenuBuilder().WithCustomId(customId).WithPlaceholder("Select new system role...");
            foreach (var role in assignableRoles)
                menuBuilder.AddOption(label: role.Name, value: role.Id.ToString(), description: $"Hierarchy weight: {role.HierarchyWeight}", isDefault: role.Id == currentRoleId);

            return menuBuilder;
        }

        public SelectMenuBuilder GetAdminStatusMenuBuilder(string customId, bool currentState)
        {
            return new SelectMenuBuilder().WithCustomId(customId).WithPlaceholder("Select administrator status...")
                .AddOption("Enable Administrator", DiscordComponentActions.StatusTrue, "Administrator can issue bot commands.", isDefault: currentState, emote: _emoteCache.GetEmote("UI_ICON_USER_CHECK"))
                .AddOption("Disable Administrator", DiscordComponentActions.StatusFalse, "Administrator is blocked from bot interaction.", isDefault: !currentState, emote: _emoteCache.GetEmote("UI_ICON_USER_LOCK"));
        }
    }
}