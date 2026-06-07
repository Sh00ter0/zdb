using Application.Repositories;
using Discord;
using Domain.Entities;
using Infrastructure.Exceptions;

namespace Infrastructure.Discord.SlashCommands.Commands.System.Administration;

public sealed class AdministrationPanelRenderer(
    ISystemAdministratorRepository adminRepository,
    AdministrationUiBuilder uiBuilder)
{
    public MessageComponent CreateManagementPanel(SystemAdministrators targetAdmin, IUser targetDiscordUser,
        AppInteractionContext context)
    {
        var actionMenu = uiBuilder.GetActionMenuBuilder($"admin_select_action:{targetAdmin.DiscordUserId}", targetAdmin, context.Admin!);

        return uiBuilder.CreateOverviewContainer(targetAdmin, targetDiscordUser, cb =>
        {
            cb.WithActionRow(row => row.AddComponent(actionMenu));
        });
    }

    public async Task<(SystemAdministrators Admin, IUser User, MessageComponent Components)> CreateManagementPanelAsync(
        AppInteractionContext context, ulong targetDiscordId)
    {
        var targetAdmin = await adminRepository.GetByDiscordIdAsync(targetDiscordId);
        if (targetAdmin == null) throw new UserVisibleException("Administrator not found.");

        var targetDiscordUser = (context.Client.GetUser(targetDiscordId) as IUser) ?? await context.Client.Rest.GetUserAsync(targetDiscordId);
        if (targetDiscordUser == null) throw new UserVisibleException("Could not fetch user from Discord API.");

        var components = CreateManagementPanel(targetAdmin, targetDiscordUser, context);

        return (targetAdmin, targetDiscordUser, components);
    }
}
