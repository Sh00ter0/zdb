using Application.Repositories;
using Discord;
using Domain.Entities;
using Infrastructure.Exceptions;

namespace Infrastructure.Discord.SlashCommands.Commands.Api.Clients;

public sealed class ApiClientPanelRenderer(
    IIntegrationClientRepository apiClientRepository,
    ApiClientUiBuilder uiBuilder)
{
    public MessageComponent CreateManagementPanel(IntegrationClients client, AppInteractionContext context)
    {
        var userPermissions = context.Admin!.Role.RolePermissions.Select(rp => rp.Permission.Key).ToList();
        var actionMenu = uiBuilder.GetManagementMenuBuilder($"client_select_action:{client.Id}", userPermissions);

        return uiBuilder.CreateOverviewContainer(client, cb =>
        {
            cb.WithActionRow(row => row.AddComponent(actionMenu));
        });
    }

    public async Task<MessageComponent> CreateManagementPanelAsync(long clientId, AppInteractionContext context)
    {
        var client = await apiClientRepository.GetByIdAsync(clientId);
        if (client == null) throw new InteractionException("Client not found.");

        return CreateManagementPanel(client, context);
    }
}
