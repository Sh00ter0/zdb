using Application.Repositories;
using Discord;
using Domain.Entities;
using Infrastructure.Exceptions;

namespace Infrastructure.Discord.SlashCommands.Commands.Api.KnownTargets;

public sealed class KnownTargetPanelRenderer(
    IIntegrationClientRepository apiClientRepository,
    IKnownDeliveryTargetRepository targetRepository,
    KnownTargetUiBuilder uiBuilder)
{
    public MessageComponent CreateManagementPanel(IntegrationClients client, KnownDeliveryTargets target,
        AppInteractionContext context)
    {
        var userPermissions = context.Admin!.Role.RolePermissions.Select(rp => rp.Permission.Key).ToList();
        var actionMenu = uiBuilder.GetManagementMenuBuilder($"target_select_action:{client.Id}:{target.TargetId}", userPermissions);

        return uiBuilder.CreateOverviewContainer(client.Name, target, cb =>
        {
            cb.WithActionRow(row => row.AddComponent(actionMenu));
        });
    }

    public async Task<MessageComponent> CreateManagementPanelAsync(long clientId, ulong targetDiscordId,
        AppInteractionContext context)
    {
        var target = await targetRepository.GetByDiscordIdAsync(clientId, targetDiscordId);
        var client = await apiClientRepository.GetByIdAsync(clientId);
        if (target == null || client == null) throw new InteractionException("Target or client not found.");

        return CreateManagementPanel(client, target, context);
    }
}
