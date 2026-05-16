using Application.Discord.Panels.ClientPanel.Actions;
using Application.Discord.Panels.ClientPanel.States;
using Application.Discord.Panels.Core.Layout;
using Application.Discord.Panels.LayoutBuilders.ClientPanel;
using Domain.Attributes;
using Domain.Constants;
using Domain.Enums;
using System.Reflection;

namespace Application.Discord.Panels.LayoutBuilders;

/// <summary>
/// Builds the main client management layout with client details and available management actions.
/// </summary>
public sealed class ClientOverviewLayoutBuilder : IPanelLayoutBuilder<ClientOverviewState>
{
    // The enum remains the source of display metadata, while this map links each option to a panel action.
    private static readonly IReadOnlyDictionary<ApiClientModifyingAction, string> ActionIds =
        new Dictionary<ApiClientModifyingAction, string>
        {
            [ApiClientModifyingAction.ChangeName] = ClientPanelActionIds.OpenRename,
            [ApiClientModifyingAction.EnableOrDisableClient] = ClientPanelActionIds.OpenStatus,
            [ApiClientModifyingAction.RenewZabbixConnection] = ClientPanelActionIds.OpenZabbix,
            [ApiClientModifyingAction.DisplayRelatedTargets] = ClientPanelActionIds.OpenTargets,
            [ApiClientModifyingAction.RenewApiKey] = ClientPanelActionIds.OpenRegenerateKey,
            [ApiClientModifyingAction.Remove] = ClientPanelActionIds.OpenDeleteWarning
        };

    /// <inheritdoc />
    public PanelLayout Build(ClientOverviewState state)
    {
        var clientId = state.Client.Id;
        var createdAt = $"<t:{((DateTimeOffset)state.Client.CreatedAtUtc).ToUnixTimeSeconds()}:F>";
        var updatedAt = state.Client.UpdatedAtUtc != null
            ? $"<t:{((DateTimeOffset)state.Client.UpdatedAtUtc).ToUnixTimeSeconds()}:F>"
            : "`N/A`";
        var status = state.Client.IsActive
            ? "{emote:UI_ICON_BULB_ON}`Active`"
            : "{emote:UI_ICON_BULB_OFF}`Disabled`";
        var zabbixUrl = string.IsNullOrEmpty(state.Client.ZabbixCredential?.ApiUrl)
            ? "`Not Configured`"
            : $"`{state.Client.ZabbixCredential.ApiUrl}`";

        var body = $"""
            **Client Name:** `{state.Client.Name}`
            **Status:** {status}
            **Key Preview:** `{state.Client.KeyPreview}`
            **Zabbix URL:** {zabbixUrl}
            **Created At:** {createdAt}
            **Updated At:** {updatedAt}
            """;

        var components = new List<IUiComponent>
        {
            new SeparatorComponent(SeparatorSize.Large),
            new TextComponent(body),
            new SeparatorComponent(SeparatorSize.Small, IsDivider: false)
        };

        if (!string.IsNullOrEmpty(state.NewGeneratedKey))
        {
            components.Add(new TextComponent($"""
                {("\U0001F512")} **NEW API KEY GENERATED:**
                `{state.NewGeneratedKey}`

                *Important: Copy and store this key now.*
                """));
        }

        components.Add(new ActionRowComponent(
        [
            new SelectMenuComponent(
                Placeholder: "Select a client action to perform...",
                Action: ClientPanelLayout.Action(ClientPanelActionIds.OpenRename, clientId),
                Options: BuildManagementOptions(clientId))
        ]));

        return new PanelLayout
        {
            Components =
            [
                new ContainerComponent(
                    Header: "Manage API Client",
                    Components: components,
                    AccentColor: AppColors.Info,
                    FooterSeparatorSize: SeparatorSize.Small)
            ]
        };
    }

    /// <summary>
    /// Builds select menu options for every supported client management action.
    /// </summary>
    /// <param name="clientId">The client identifier encoded into each option action.</param>
    /// <returns>The select menu options displayed in the overview action menu.</returns>
    private static IReadOnlyList<SelectMenuOptionComponent> BuildManagementOptions(long clientId)
    {
        var options = new List<SelectMenuOptionComponent>();

        foreach (var action in Enum.GetValues<ApiClientModifyingAction>())
        {
            if (!ActionIds.TryGetValue(action, out var actionId))
                continue;

            var optionInfo = GetOptionInfo(action);
            if (optionInfo is null)
                continue;

            options.Add(new SelectMenuOptionComponent
            {
                Label = optionInfo.Label,
                Description = optionInfo.Description,
                EmoteName = optionInfo.Emote,
                Action = ClientPanelLayout.Action(actionId, clientId)
            });
        }

        return options;
    }

    /// <summary>
    /// Reads Discord select menu metadata from the action enum member.
    /// </summary>
    /// <param name="action">The action whose display metadata should be loaded.</param>
    /// <returns>The configured select option metadata, or <see langword="null" /> when none exists.</returns>
    private static DiscordSelectOptionAttribute? GetOptionInfo(ApiClientModifyingAction action)
    {
        var member = typeof(ApiClientModifyingAction)
            .GetMember(action.ToString())
            .FirstOrDefault();

        return member?.GetCustomAttribute<DiscordSelectOptionAttribute>();
    }
}
