using Application.Discord.Panels.ClientPanel.States;
using Application.Discord.Panels.Core.Layout;
using Domain.Constants;
using System.Text;

namespace Application.Discord.Panels.LayoutBuilders.ClientPanel;

/// <summary>
/// Builds the client panel layout that lists known delivery targets assigned to the API client.
/// </summary>
public sealed class ClientTargetsLayoutBuilder : IPanelLayoutBuilder<ClientTargetsState>
{
    /// <inheritdoc />
    public PanelLayout Build(ClientTargetsState state)
    {
        var body = new StringBuilder();

        if (state.Targets.Count == 0)
        {
            body.AppendLine("> *No targets are currently configured for this client.*");
        }
        else
        {
            foreach (var target in state.Targets)
            {
                var autoPublish = target.AutoCrosspost ? "\u2705" : "\u274C";
                body.AppendLine($"- **{target.Name}** (`{target.TargetId}`) \u2014 Auto-Publish: {autoPublish}");
            }
        }

        body.AppendLine();
        body.AppendLine("*To manage these targets in detail, use the `/api known-target manage` slash command.*");

        return new PanelLayout
        {
            Components =
            [
                ClientPanelLayout.StandardContainer(
                    header: $"Known Targets ({state.Targets.Count})",
                    body: body.ToString(),
                    accentColor: AppColors.Info,
                    controls:
                    [
                        new ActionRowComponent(
                        [
                            ClientPanelLayout.ReturnButton(state.Client.Id)
                        ])
                    ])
            ]
        };
    }
}
