using Discord;
using Discord.Interactions;

namespace Infrastructure.Discord.SlashCommands.Commands.Api.KnownTargets.Actions;

public sealed class TargetCancelAction(KnownTargetPanelRenderer panelRenderer)
{
    public async Task ExecuteAsync(DiscordInteractionView module, long clientId,
        ulong targetDiscordId)
    {
        var components = await panelRenderer.CreateManagementPanelAsync(clientId, targetDiscordId, module.Context);

        await ((IComponentInteraction)module.Context.Interaction).UpdateAsync(msg => msg.Components = components);
    }
}
