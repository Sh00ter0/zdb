using Application.Discord.Panels.ClientPanel.Actions;
using Application.Discord.Panels.ClientPanel.States;
using Application.Discord.Panels.Core.Layout;
using Domain.Constants;

namespace Application.Discord.Panels.LayoutBuilders.ClientPanel;

/// <summary>
/// Builds the terminal client panel layout shown after an API client has been removed.
/// </summary>
public sealed class ClientDeletedLayoutBuilder : IPanelLayoutBuilder<ClientDeletedState>
{
    /// <inheritdoc />
    public PanelLayout Build(ClientDeletedState state)
    {
        return new PanelLayout
        {
            Components =
            [
                ClientPanelLayout.StandardContainer(
                    header: "\u274C Client Removed",
                    body: "Api client and all associated targets have been permanently removed.",
                    accentColor: AppColors.Error,
                    controls:
                    [
                        new ActionRowComponent(
                        [
                            new ButtonComponent(
                                Label: "Close Window",
                                Action: new PanelActionDescriptor("client", ClientPanelActionIds.ClosePanel),
                                Style: ButtonStyleType.Danger)
                        ])
                    ])
            ]
        };
    }
}
