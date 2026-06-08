using Application.Repositories;
using Application.Services.Discord;
using Application.Services.Pagination;
using Discord;
using Discord.Interactions;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Discord.SlashCommands.Commands.System.Administration.Actions;
using Infrastructure.Exceptions;
using Infrastructure.Extensions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Discord.SlashCommands.Commands.System.Administration;

public sealed class AdministrationCommandsBackend(
    ILogger<AdministrationCommandsBackend> logger,
    ISystemAdministratorRepository adminRepository,
    IDiscordUiService discordUiService,
    IPaginationService paginationService,
    IDiscordEmoteService emoteCache,
    IDbContextFactory<ApiSecurityDbContext> dbFactory,
    AdministrationPanelRenderer panelRenderer,
    AdministrationChangeRoleAction changeRoleAction,
    AdministrationStatusAction statusAction,
    AdministrationCancelAction cancelAction)
{
    public async Task CreateAdministratorAsync(DiscordInteractionView module, IUser user,
        int roleId)
    {
        await module.DeferInteractionAsync(ephemeral: true);

        await using var db = await dbFactory.CreateDbContextAsync();
        var selectedRole = await db.SystemRoles.FindAsync(roleId);
        if (selectedRole == null) throw new Exceptions.InteractionException("The specified role ID does not exist in the system.");

        if (module.Context.Admin!.Role.HierarchyWeight <= selectedRole.HierarchyWeight)
        {
            throw new Exceptions.InteractionException("You can only assign roles that are strictly lower than your own hierarchy weight.");
        }

        if (user.Id == module.Context.User.Id)
        {
            throw new Exceptions.InteractionException("You cannot manage your own administrative status.");
        }

        if (user.IsBot)
        {
            throw new Exceptions.InteractionException("Bots cannot be registered as system administrators.");
        }

        var existingAdmin = await adminRepository.GetByDiscordIdAsync(user.Id);
        if (existingAdmin != null)
        {
            throw new Exceptions.InteractionException($"User <@{user.Id}> is already registered in the system.");
        }

        var newAdmin = new SystemAdministrators
        {
            DiscordUserId = user.Id,
            CreatedById = module.Context.Admin!.Id,
            RoleId = roleId,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var success = await adminRepository.AddAsync(newAdmin);
        if (!success) throw new Exceptions.InteractionException("An internal database error occurred while creating the administrator.");

        var container = discordUiService.CreateStandardContainer(
            header: "Administrator Created",
            body: $"User <@{user.Id}> has been successfully granted access.\n-# Role: {selectedRole.Name}",
            accentColor: AppColors.Success);

        var welcomeMessageContainer = discordUiService.CreateStandardContainer(
            header: "Welcome to the Administration Team!",
            body: $"""
            Hello <@{user.Id}>,
            
            You have been added as a system user with the `{selectedRole.Name}` role.
            Please familiarize yourself with the available commands and use your permissions responsibly.
            If you have any questions, feel free to reach out to higher-tier administrators.
            """,
            accentColor: AppColors.Success);

        try
        {
            await user.SendMessageAsync(components: welcomeMessageContainer);
        }
        catch
        {
        }

        await module.FollowupInteractionAsync(components: container, ephemeral: true, flags: MessageFlags.ComponentsV2);
        logger.LogInformation("Admin {CreatorId} created new user {NewUserId} with Role ID {Role}", module.Context.User.Id, user.Id, roleId);
    }

    public async Task ListAdministratorsAsync(DiscordInteractionView module)
    {
        await module.DeferInteractionAsync(ephemeral: true);

        var dbAdmins = await adminRepository.GetAllAsync();

        var adminDataList = new List<(SystemAdministrators Entity, IUser? DiscordUser, string Username)>();

        foreach (var admin in dbAdmins)
        {
            var discordUser = (module.Context.Client.GetUser(admin.DiscordUserId) as IUser) ?? await module.Context.Client.Rest.GetUserAsync(admin.DiscordUserId);
            var username = discordUser?.Username ?? "Unknown User";
            adminDataList.Add((admin, discordUser, username));
        }

        var sortedAdmins = adminDataList
            .OrderByDescending(a => a.Entity.Role.HierarchyWeight)
            .ThenBy(a => a.Username)
            .ToList();

        var items = new List<string>();

        foreach (var item in sortedAdmins)
        {
            var usernameDisplay = item.DiscordUser != null ? $"**{item.DiscordUser.Username}**" : "*Unknown User*";
            var statusEmoteName = item.Entity.IsActive ? IsActive.True.GetDiscordEmote() : IsActive.False.GetDiscordEmote();
            var statusIcon = statusEmoteName is { } emoteName ? emoteCache.GetEmote(emoteName) : null;
            var discordCreatedAtTimestamp = $"<t:{((DateTimeOffset)item.Entity.CreatedAtUtc).ToUnixTimeSeconds()}:F>";
            var discordUpdatedAtTimestamp = item.Entity.UpdatedAtUtc.HasValue ? $"<t:{((DateTimeOffset)item.Entity.UpdatedAtUtc.Value).ToUnixTimeSeconds()}:F>" : "`N/A`";

            var bodyText = $"""
                {usernameDisplay} (`{item.Entity.DiscordUserId}`)
                ├ **Role:** `{item.Entity.Role.Name}`
                ├ **Status:** {statusIcon} {(item.Entity.IsActive ? "Active" : "Disabled")}
                ├ **Protected:** {(item.Entity.IsSystemManaged ? "Yes" : "No")}
                ├ **CreatedAt:** {discordCreatedAtTimestamp}
                └ **UpdatedAt:** {discordUpdatedAtTimestamp}
                """;
            items.Add(bodyText);
        }

        var sessionId = paginationService.CreatePaginationSession(
            header: $"System Administrators\n-# Total registered: {dbAdmins.Count}",
            items: items,
            charsPerPage: 1200,
            separator: "\n\n"
        );

        var sessionData = paginationService.GetSessionData(sessionId);
        if (sessionData == null || sessionData.Pages.Count == 0) throw new Exceptions.InteractionException("Failed to generate administrator list.");

        var listComponents = discordUiService.CreatePaginatedContainer(
            header: sessionData.Header,
            pageText: sessionData.Pages[0],
            currentPage: 1,
            totalPages: sessionData.Pages.Count,
            sessionId: sessionId,
            customActionBtn: sessionData.CustomButton
        );

        await module.FollowupInteractionAsync(components: listComponents, ephemeral: true, flags: MessageFlags.ComponentsV2);
    }

    public async Task ManageAdministratorAsync(DiscordInteractionView module,
        IUser targetUser)
    {
        var targetAdmin = await adminRepository.GetByDiscordIdAsync(targetUser.Id);
        if (targetAdmin == null) throw new Exceptions.InteractionException($"User <@{targetUser.Id}> is not an administrator.");

        var components = panelRenderer.CreateManagementPanel(targetAdmin, targetUser, module.Context);

        await module.RespondInteractionAsync(components: components, ephemeral: true, flags: MessageFlags.ComponentsV2);
    }

    public async Task HandleAdminActionSelectAsync(DiscordInteractionView module,
        ulong targetDiscordId, string[] selectedValues)
    {
        var action = Enum.Parse<BotAdminAction>(selectedValues[0]);
        var targetAdmin = await adminRepository.GetByDiscordIdAsync(targetDiscordId);
        if (targetAdmin == null) throw new Exceptions.InteractionException("Administrator not found.");

        if (module.Context.User.Id == targetDiscordId)
            throw new Exceptions.InteractionException("You cannot modify your own administrative status.");

        if (module.Context.Admin!.Role.HierarchyWeight <= targetAdmin.Role.HierarchyWeight)
            throw new Exceptions.InteractionException("You can only manage users with a hierarchy strictly lower than your own.");

        var targetDiscordUser = (module.Context.Client.GetUser(targetDiscordId) as IUser) ?? await module.Context.Client.Rest.GetUserAsync(targetDiscordId);
        if (targetDiscordUser == null) throw new Exceptions.InteractionException("Could not fetch user from Discord API.");

        switch (action)
        {
            case BotAdminAction.ChangeUserRole:
                await changeRoleAction.ShowPanelAsync(module, targetAdmin, targetDiscordUser);
                break;

            case BotAdminAction.EnableOrDisableUser:
                await statusAction.ShowPanelAsync(module, targetAdmin, targetDiscordUser);
                break;
        }
    }

    public Task HandleSetRoleAsync(DiscordInteractionView module, ulong targetDiscordId,
        string[] selectedValues)
    {
        return changeRoleAction.HandleSelectAsync(module, targetDiscordId, selectedValues);
    }

    public Task HandleSetStatusAsync(DiscordInteractionView module, ulong targetDiscordId,
        string[] selectedValues)
    {
        return statusAction.HandleSelectAsync(module, targetDiscordId, selectedValues);
    }

    public Task HandleAdminCancelAsync(DiscordInteractionView module, ulong targetDiscordId)
    {
        return cancelAction.ExecuteAsync(module, targetDiscordId);
    }
}
