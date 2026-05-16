using Application.Discord.Panels.ClientPanel.Actions;
using Application.Discord.Panels.ClientPanel.States;
using Application.Discord.Panels.Core.Layout;
using Domain.Constants;

namespace Application.Discord.Panels.LayoutBuilders.ClientPanel;

/// <summary>
/// Builds confirmation layouts for destructive or sensitive client panel actions.
/// </summary>
public sealed class ClientWarningLayoutBuilder : IPanelLayoutBuilder<ClientWarningState>
{
    /// <inheritdoc />
    public PanelLayout Build(ClientWarningState state)
    {
        var isDeleteWarning = state.WarningType == "Delete";
        var submitAction = isDeleteWarning
            ? ClientPanelActionIds.DeleteSubmit
            : ClientPanelActionIds.RenewSubmit;
        var submitStyle = isDeleteWarning
            ? ButtonStyleType.Danger
            : ButtonStyleType.Success;
        var submitLabel = isDeleteWarning
            ? "Yes, delete it"
            : "Yes, regenerate key";

        return new PanelLayout
        {
            Components =
            [
                ClientPanelLayout.StandardContainer(
                    header: "\u26A0\uFE0F Action Required",
                    body: state.WarningMessage,
                    accentColor: AppColors.Warning,
                    controls:
                    [
                        new ActionRowComponent(
                        [
                            new ButtonComponent(
                                Label: submitLabel,
                                Action: ClientPanelLayout.Action(submitAction, state.Client.Id),
                                Style: submitStyle),
                            ClientPanelLayout.ReturnButton(state.Client.Id, "Cancel")
                        ])
                    ])
            ]
        };
    }
}
