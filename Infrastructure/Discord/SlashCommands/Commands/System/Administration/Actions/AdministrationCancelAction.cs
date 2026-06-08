using Discord;
using Discord.Interactions;

namespace Infrastructure.Discord.SlashCommands.Commands.System.Administration.Actions;

public sealed class AdministrationCancelAction(AdministrationPanelRenderer panelRenderer)
{
    public async Task ExecuteAsync(DiscordInteractionView module, ulong targetDiscordId)
    {
        var result = await panelRenderer.CreateManagementPanelAsync(module.Context, targetDiscordId);

        await ((IComponentInteraction)module.Context.Interaction).UpdateAsync(msg => msg.Components = result.Components);
    }
}
