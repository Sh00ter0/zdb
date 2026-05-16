using Application.Common.Constants;
using Application.Discord.Panels.ClientPanel.Actions;
using Application.Discord.Panels.ClientPanel.States;
using Application.Discord.Panels.Core.Layout;
using Application.Discord.Panels.LayoutBuilders.ClientPanel;
using Domain.Constants;

namespace Application.Discord.Panels.LayoutBuilders;

/// <summary>
/// Builds the client status management layout with enabled and disabled status options.
/// </summary>
public sealed class ClientStatusLayoutBuilder : IPanelLayoutBuilder<ClientStatusState>
{
    /// <inheritdoc />
    public PanelLayout Build(ClientStatusState state)
    {
        var clientId = state.Client.Id;

        return new PanelLayout
        {
            Components =
            [
                ClientPanelLayout.StandardContainer(
                    header: "Manage Status",
                    body: "Select the new operational status for this API client.",
                    accentColor: AppColors.Info,
                    controls:
                    [
                        new ActionRowComponent(
                        [
                            new SelectMenuComponent(
                                Placeholder: "Select client status...",
                                Action: ClientPanelLayout.Action(ClientPanelActionIds.ToggleStatus, clientId),
                                Options:
                                [
                                    new SelectMenuOptionComponent
                                    {
                                        Label = "Enabled",
                                        Value = DiscordComponentActions.StatusTrue,
                                        Description = "Client is active and processing requests",
                                        IsDefault = state.Client.IsActive,
                                        EmoteName = "UI_ICON_BULB_ON"
                                    },
                                    new SelectMenuOptionComponent
                                    {
                                        Label = "Disabled",
                                        Value = DiscordComponentActions.StatusFalse,
                                        Description = "Client is inactive and will reject requests",
                                        IsDefault = !state.Client.IsActive,
                                        EmoteName = "UI_ICON_BULB_OFF"
                                    }
                                ])
                        ]),
                        new ActionRowComponent(
                        [
                            ClientPanelLayout.ReturnButton(clientId)
                        ])
                    ])
            ]
        };
    }
}
