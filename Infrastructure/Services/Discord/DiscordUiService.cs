using Application.Common.Zabbix;
using Application.Services.Discord;
using Discord;
using Discord.WebSocket;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Extensions;
using Application.Views.Components;
using Infrastructure.Views.Layouts;

namespace Infrastructure.Services.Discord
{
    public class DiscordUiService(DiscordSocketClient client, IDiscordEmoteService emoteCache)
        : IDiscordUiService
    {
        public MessageComponent CreateStandardContainer(string header, string body, Color? accentColor = null,
            string? footerNote = null)
        {
            var layout = new StandardLayout(emoteCache, client)
                .Create(header);

            if (accentColor != null) layout.WithAccentColor(accentColor.Value);

            layout.AddSection(
                new TextSection(body)
            );

            return layout.Build();
        }

        public Modal CreateConfirmationModal(string customId, string title, string inputLabel, string placeholder,
            int maxLength)
        {
            var mb = new ModalBuilder()
                .WithTitle(title)
                .WithCustomId(customId)
                .AddTextInput(label: inputLabel, customId: "confirm_text", style: TextInputStyle.Short,
                    placeholder: placeholder, required: true, maxLength: maxLength);
            return mb.Build();
        }

        public MessageComponent CreatePaginatedContainer(string header, string pageText, int currentPage,
            int totalPages, string sessionId, Color? accentColor = null, ButtonBuilder? customActionBtn = null)
        {
            var layout = new StandardLayout(emoteCache, client)
                .Create(header);
            if (accentColor != null) layout.WithAccentColor(accentColor.Value);

            layout.AddSection(
                new TextSection(pageText)
            );

            if (totalPages > 1)
            {
                layout.AddSection(
                    new PaginationSection(sessionId, currentPage, totalPages)
                );
            }

            if (customActionBtn is not null)
            {
                layout.AddSection(
                    new ActionSection([customActionBtn])
                );
            }

            return layout.Build();
        }

        public MessageComponent CreateZabbixAlertContainer(ZabbixPayload payload, bool isDm, long apiClientId)
        {
            Color containerColor;
            if (payload.EventSource is 1 or 2) containerColor = AppColors.Info;
            else
            {
                var isResolved = payload.EventValue == 0;
                if (isResolved) containerColor = AppColors.Success;
                else
                {
                    containerColor = payload.Severity switch
                    {
                        1 => AppColors.SeverityInformation,
                        2 => AppColors.SeverityWarning,
                        3 => AppColors.SeverityAverage,
                        4 => AppColors.SeverityHigh,
                        5 => AppColors.SeverityDisaster,
                        _ => AppColors.SeverityNotClassified
                    };
                }
            }

            var safeMessage = payload.Message.Length >= 3900
                ? string.Concat(payload.Message.AsSpan(0, 3900), "…")
                : payload.Message;

            var layout = new StandardLayout(emoteCache, client);
            layout.Create(payload.Subject)
                .WithAccentColor(containerColor)
                .AddSection(
                    new TextSection(safeMessage)
                );


            if (!isDm || payload.ControlMenu != 1 || payload.EventValue == 0 || payload.EventSource == 1 ||
                payload.EventSource == 2) return layout.Build();

            var pulseIcon = emoteCache.GetEmote("UI_ICON_PULSE");
            var actionButton = new ButtonBuilder()
                .WithCustomId($"btn_manage:{apiClientId}:{payload.EventId}")
                .WithLabel("Take action")
                .WithStyle(ButtonStyle.Primary)
                .WithEmote(pulseIcon);

            layout.AddSection(
                new ActionSection([actionButton])
            );

            return layout.Build();
        }

        public SelectMenuBuilder GetZabbixAckMenuBuilder(string customId, bool currentState)
        {
            var trueIcon = emoteCache.GetEmote("UI_ICON_CHECK");
            var falseIcon = emoteCache.GetEmote("UI_ICON_X");
            return new SelectMenuBuilder().WithCustomId(customId).WithPlaceholder("Acknowledgment Status...")
                .AddOption("Acknowledged", "true", "Mark event as acknowledged", isDefault: currentState,
                    emote: trueIcon)
                .AddOption("Not Acknowledged", "false", "Leave event unacknowledged", isDefault: !currentState,
                    emote: falseIcon);
        }

        public SelectMenuBuilder GetZabbixSeverityMenuBuilder(string customId, int currentSeverity)
        {
            var sevMenu = new SelectMenuBuilder().WithCustomId(customId).WithPlaceholder("Update Severity...");
            foreach (ZabbixSeverity severity in Enum.GetValues(typeof(ZabbixSeverity)))
            {
                var optionInfo = severity.GetDiscordOptionInfo();
                if (optionInfo == null) continue;
                sevMenu.AddOption(label: optionInfo.Label, value: ((int)severity).ToString(),
                    description: optionInfo.Description, emote: emoteCache.GetEmote(optionInfo.Emote),
                    isDefault: (int)severity == currentSeverity);
            }

            return sevMenu;
        }

        public MessageComponent CreateZabbixManagementPanel(string eventId, SelectMenuBuilder ackMenu,
            SelectMenuBuilder sevMenu, ButtonBuilder commentBtn)
        {
            var layout = new StandardLayout(emoteCache, client)
                .Create("Manage Event")
                .AddSection(
                    new ActionSection([ackMenu, sevMenu, commentBtn])
                );

            return layout.Build();
        }

        public Modal CreateZabbixCommentModal(string customId)
        {
            return new ModalBuilder().WithCustomId(customId)
                .WithTitle("Add Comment")
                .AddTextInput("Comment","comment_text", TextInputStyle.Paragraph, "Enter your comment here...", required: true)
                .Build();
        }

        public MessageComponent CreateApiClientOverviewContainer(IntegrationClients client1,
            Action<ContainerBuilder> appendComponents = null!)
        {
            var bulbIconON = emoteCache.GetEmote("UI_ICON_BULB_ON");
            var bulbIconOFF = emoteCache.GetEmote("UI_ICON_BULB_OFF");
            var discordCreateTimestamp = $"<t:{((DateTimeOffset)client1.CreatedAtUtc).ToUnixTimeSeconds()}:F>";
            var discordUpdateTimestamp = client1.UpdatedAtUtc != null
                ? $"<t:{((DateTimeOffset)client1.UpdatedAtUtc).ToUnixTimeSeconds()}:F>"
                : "`N/A`";
            string statusIcon = client1.IsActive ? (bulbIconON + "`Active`") : (bulbIconOFF + "`Disabled`");
            string zabbixUrl = string.IsNullOrEmpty(client1.ZabbixCredential?.ApiUrl)
                ? "`Not Configured`"
                : $"`{client1.ZabbixCredential.ApiUrl}`";

            var bodyText = $"""
                            **Client Name:** `{client1.Name}`
                            **Status:** {statusIcon}
                            **Key Preview:** `{client1.KeyPreview}`
                            **Zabbix URL:** {zabbixUrl}
                            **Created At:** {discordCreateTimestamp}
                            **Updated At:** {discordUpdateTimestamp}
                            """;

            return SimpleMessageWithAction("Manage API Client", bodyText, appendComponents);
        }

        public SelectMenuBuilder GetApiClientManagementMenuBuilder(string customId, List<string> userPermissions)
        {
            var menuBuilder = new SelectMenuBuilder().WithCustomId(customId)
                .WithPlaceholder("Select a client action to perform...");
            bool isRoot = userPermissions.Contains("root");

            foreach (ApiClientModifyingAction action in Enum.GetValues(typeof(ApiClientModifyingAction)))
            {
                var optionInfo = action.GetDiscordOptionInfo();
                if (optionInfo == null) continue;

                if (!isRoot && optionInfo.RequiredPermission != null &&
                    !userPermissions.Contains(optionInfo.RequiredPermission)) continue;

                menuBuilder.AddOption(label: optionInfo.Label, value: action.ToString(),
                    description: optionInfo.Description, emote: emoteCache.GetEmote(optionInfo.Emote));
            }

            if (menuBuilder.Options.Count == 0)
            {
                menuBuilder.WithPlaceholder("No actions available").AddOption("Insufficient permissions", "none",
                    "You do not have permission to manage this.").WithDisabled(true);
            }

            return menuBuilder;
        }

        public SelectMenuBuilder GetClientStatusSelectMenuBuilder(string customId, bool currentState)
        {
            var onIcon = emoteCache.GetEmote("UI_ICON_BULB_ON");
            var offIcon = emoteCache.GetEmote("UI_ICON_BULB_OFF");
            return new SelectMenuBuilder().WithCustomId(customId).WithPlaceholder("Select client status...")
                .AddOption("Enabled", "true", "Client is active and processing requests", isDefault: currentState,
                    emote: onIcon)
                .AddOption("Disabled", "false", "Client is inactive and will reject requests", isDefault: !currentState,
                    emote: offIcon);
        }

        public MessageComponent CreateTargetOverviewContainer(string clientName, KnownDeliveryTargets target,
            Action<ContainerBuilder>? appendComponents = null)
        {
            var discordCreatedAtTimestamp = $"<t:{((DateTimeOffset)target.CreatedAtUtc).ToUnixTimeSeconds()}:F>";
            var discordUpdatedAtTimestamp = target.UpdatedAtUtc != null
                ? $"<t:{((DateTimeOffset)target.UpdatedAtUtc).ToUnixTimeSeconds()}:F>"
                : "`N/A`";
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

            return SimpleMessageWithAction($"Manage Target: {clientName}", bodyText, appendComponents);
        }

        public SelectMenuBuilder GetTargetManagementMenuBuilder(string customId, List<string> userPermissions)
        {
            var menuBuilder = new SelectMenuBuilder().WithCustomId(customId)
                .WithPlaceholder("Select an action to perform...");
            bool isRoot = userPermissions.Contains("root");

            foreach (AllowedTargetModifyingAction action in Enum.GetValues(typeof(AllowedTargetModifyingAction)))
            {
                var optionInfo = action.GetDiscordOptionInfo();
                if (optionInfo == null) continue;

                if (!isRoot && optionInfo.RequiredPermission != null &&
                    !userPermissions.Contains(optionInfo.RequiredPermission)) continue;

                menuBuilder.AddOption(label: optionInfo.Label, value: action.ToString(),
                    description: optionInfo.Description, emote: emoteCache.GetEmote(optionInfo.Emote));
            }

            if (menuBuilder.Options.Count == 0)
            {
                menuBuilder.WithPlaceholder("No actions available").AddOption("Insufficient permissions", "none",
                    "You do not have permission to manage this.").WithDisabled(true);
            }

            return menuBuilder;
        }

        public SelectMenuBuilder GetCrosspostSelectMenuBuilder(string customId, bool currentState)
        {
            var trueIcon = emoteCache.GetEmote("UI_ICON_CHECK");
            var falseIcon = emoteCache.GetEmote("UI_ICON_X");
            return new SelectMenuBuilder().WithCustomId(customId).WithPlaceholder("Select auto-publish mode...")
                .AddOption("Enable Auto-Publish", "true", "Messages will be automatically published",
                    isDefault: currentState, emote: trueIcon)
                .AddOption("Disable Auto-Publish", "false", "Messages will NOT be automatically published",
                    isDefault: !currentState, emote: falseIcon);
        }

        public MessageComponent CreateAdminOverviewContainer(SystemAdministrators adminEntity, IUser discordUser,
            Action<ContainerBuilder>? appendComponents = null)
        {
            var activeIcon = emoteCache.GetEmote("UI_ICON_BULB_ON");
            var inactiveIcon = emoteCache.GetEmote("UI_ICON_BULB_OFF");
            string discordCreatedAtTimestamp =
                $"<t:{((DateTimeOffset)adminEntity.CreatedAtUtc).ToUnixTimeSeconds()}:F>";
            string discordUpdatedAtTimestamp = adminEntity.UpdatedAtUtc != null
                ? $"<t:{((DateTimeOffset)adminEntity.UpdatedAtUtc.Value).ToUnixTimeSeconds()}:F>"
                : "`N/A`";
            IEmote? statusIcon = adminEntity.IsActive ? activeIcon : inactiveIcon;

            var bodyText = $"""
                            **User:** {discordUser.Mention} (`{discordUser.Id}`)
                            **Role:** `{adminEntity.Role.Name}` *(Weight: {adminEntity.Role.HierarchyWeight})*
                            **Status:** {statusIcon} {(adminEntity.IsActive ? "`Active`" : "`Disabled`")}
                            **System Managed:** {(adminEntity.IsSystemManaged ? "`Yes` (Protected)" : "`No`")}
                            **Created At:** {discordCreatedAtTimestamp}
                            **Updated At:** {discordUpdatedAtTimestamp}
                            """;

            return SimpleMessageWithAction("User Administration", bodyText, appendComponents);
        }

        private MessageComponent SimpleMessageWithAction(string header, string body,
            Action<ContainerBuilder>? appendComponents)
        {
            var layout = new StandardLayout(emoteCache, client)
                .Create(header)
                .AddSection(
                    new TextSection(body)
                );

            if (appendComponents != null)
            {
                layout.AddSection(
                    new CbActionSection(appendComponents)
                );
            }

            return layout.Build();
        }

        public SelectMenuBuilder GetAdminActionMenuBuilder(string customId, SystemAdministrators targetAdmin,
            SystemAdministrators requestingAdmin)
        {
            var menuBuilder = new SelectMenuBuilder().WithCustomId(customId)
                .WithPlaceholder("Select an administrative action...");
            bool isSelf = targetAdmin.DiscordUserId == requestingAdmin.DiscordUserId;

            if (isSelf || requestingAdmin.Role.HierarchyWeight <= targetAdmin.Role.HierarchyWeight ||
                targetAdmin.IsSystemManaged)
            {
                menuBuilder.WithPlaceholder("No actions available")
                    .AddOption("Protected / Insufficient permissions", "none", "Account is immutable.")
                    .WithDisabled(true);
                return menuBuilder;
            }

            foreach (BotAdminAction action in Enum.GetValues(typeof(BotAdminAction)))
            {
                var optionInfo = action.GetDiscordOptionInfo();
                menuBuilder.AddOption(label: optionInfo?.Label ?? action.ToString(), value: action.ToString(),
                    description: optionInfo?.Description, emote: emoteCache.GetEmote(optionInfo?.Emote));
            }

            if (menuBuilder.Options.Count == 0)
            {
                menuBuilder.WithPlaceholder("No actions available")
                    .AddOption("Insufficient permissions", "none", "You cannot manage this user.").WithDisabled(true);
            }

            return menuBuilder;
        }

        public SelectMenuBuilder GetSystemRoleMenuBuilder(string customId, int currentRoleId,
            List<SystemRoles> assignableRoles)
        {
            var menuBuilder = new SelectMenuBuilder()
                .WithCustomId(customId)
                .WithPlaceholder("Select new system role...");

            foreach (var role in assignableRoles)
            {
                menuBuilder.AddOption(
                    label: role.Name,
                    value: role.Id.ToString(),
                    description: $"Hierarchy weight: {role.HierarchyWeight}",
                    isDefault: role.Id == currentRoleId);
            }

            return menuBuilder;
        }

        public SelectMenuBuilder GetAdminStatusMenuBuilder(string customId, bool currentState)
        {
            var trueIcon = emoteCache.GetEmote("UI_ICON_USER_CHECK");
            var falseIcon = emoteCache.GetEmote("UI_ICON_USER_LOCK");

            return new SelectMenuBuilder().WithCustomId(customId).WithPlaceholder("Select administrator status...")
                .AddOption("Enable Administrator", "true", "Administrator can issue bot commands.",
                    isDefault: currentState, emote: trueIcon)
                .AddOption("Disable Administrator", "false", "Administrator is blocked from bot interaction.",
                    isDefault: !currentState, emote: falseIcon);
        }
    }
}