using Application.Discord.Panels.Core.Layout;
using Application.Discord.Panels.LayoutBuilders.ClientPanel;
using Application.Discord.Panels.Shared.States;
using Domain.Constants;

namespace Application.Discord.Panels.LayoutBuilders;

/// <summary>
/// Builds the shared fallback layout shown when a panel interaction fails unexpectedly.
/// </summary>
public sealed class PanelErrorLayoutBuilder : IPanelLayoutBuilder<PanelErrorState>
{
    /// <inheritdoc />
    public PanelLayout Build(PanelErrorState state)
    {
        var body = $"""
            An unexpected error occurred while processing your request.

            **Details:** `{state.ErrorMessage}`
            **Reference ID:** `{state.ReferenceId}`
            """;

        return new PanelLayout
        {
            Components =
            [
                ClientPanelLayout.StandardContainer(
                    header: "\u26A0\uFE0F System Error",
                    body: body,
                    accentColor: AppColors.Error)
            ]
        };
    }
}
