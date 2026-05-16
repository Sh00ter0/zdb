using Application.Discord.Panels.ClientPanel.States;
using Application.Discord.Panels.Core;
using Application.Repositories;

namespace Application.Discord.Panels.ClientPanel;

/// <summary>
/// Provides the root panel implementation for API client management.
/// </summary>
public sealed class ClientPanel(
    IEnumerable<IPanelActionHandler> handlers,
    IIntegrationClientRepository repository) : ConfigPanel<IPanelViewState>
{
    /// <inheritdoc />
    public override string Id => "client";

    /// <inheritdoc />
    public override async Task<IPanelViewState> BuildStateAsync(ConfigPanelContext context)
    {
        if (!long.TryParse(context.EntityId, out var clientId))
            throw new InvalidOperationException("Invalid client ID.");

        var client = await repository.GetByIdAsync(clientId)
            ?? throw new InvalidOperationException("API client not found.");

        return new ClientOverviewState
        {
            Client = client
        };
    }

    /// <inheritdoc />
    public override async Task<PanelActionResult> ExecuteActionAsync(ConfigPanelContext context, PanelInteraction interaction)
    {
        var handler = handlers.FirstOrDefault(x => x.Action == interaction.Action);

        if (handler == null)
        {
            return new UpdatePanelResult
            {
                State = await BuildStateAsync(context)
            };
        }

        return await handler.ExecuteAsync(context, interaction);
    }
}
