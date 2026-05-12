using Application.Repositories;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Discord
{
    /// <summary>
    /// </summary>
    public class FirstRunAdminSetupService(
        ISystemAdministratorRepository adminRepository,
        DiscordSocketClient discordClient,
        ILogger<FirstRunAdminSetupService> logger)
    {
        public Task InitializeAsync()
        {
            discordClient.Ready += OnClientReadyAsync;
            return Task.CompletedTask;
        }

        private Task OnClientReadyAsync()
        {
            discordClient.Ready -= OnClientReadyAsync;

            _ = Task.Run(async () =>
            {
                try
                {
                    logger.LogInformation("Fetching application owner from Discord API...");

                    var appInfo = await discordClient.GetApplicationInfoAsync();

                    if (appInfo.Team != null)
                    {
                        logger.LogInformation("Application is owned by a Team ('{TeamName}'). Synchronizing all team members as SuperAdmins...", appInfo.Team.Name);

                        foreach (var member in appInfo.Team.TeamMembers)
                        {
                            if (member.MembershipState == MembershipState.Accepted)
                            {
                                // Korzystamy z natywnego enuma Discord.TeamRole!
                                bool isReadOnly = member.Role == TeamRole.ReadOnly;

                                if (isReadOnly)
                                {
                                    await adminRepository.UpsertSuperAdminAsync(member.User.Id, isActive: false);
                                    logger.LogInformation("Team member {MemberName} ({MemberId}) is ReadOnly. Account deactivated or skipped.", member.User.Username, member.User.Id);
                                }
                                else
                                {
                                    await adminRepository.UpsertSuperAdminAsync(member.User.Id, isActive: true);
                                    logger.LogInformation("Team member {MemberName} ({MemberId}) successfully synced.", member.User.Username, member.User.Id);
                                }
                            }
                        }
                    }
                    else
                    {
                        ulong ownerId = appInfo.Owner.Id;
                        logger.LogInformation("Application is owned by a single user ({OwnerId}). Synchronizing SuperAdmin record...", ownerId);

                        await adminRepository.UpsertSuperAdminAsync(ownerId, isActive: true);
                        logger.LogInformation("SuperAdmin successfully synced with the application owner.");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "A critical error occurred while synchronizing the Super Administrator account.");
                }
            });

            return Task.CompletedTask;
        }
    }
}
