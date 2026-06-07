using Discord;
using Discord.Interactions;

namespace Infrastructure.Discord.SlashCommands.Commands.Api.Clients.Actions;

public sealed class ClientCancelAction(ApiClientPanelRenderer panelRenderer)
{
    public async Task ExecuteAsync(DiscordInteractionView module, long clientId)
    {
        var components = await panelRenderer.CreateManagementPanelAsync(clientId, module.Context);

        await ((IComponentInteraction)module.Context.Interaction).UpdateAsync(msg => msg.Components = components);
    }
}
