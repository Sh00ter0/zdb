using Application.Services.API;
using Infrastructure.Discord.Events;
using Infrastructure.Services.Discord;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    /// <summary>
    /// </summary>
    public class StartupService(
        DiscordStartupService discord,
        InteractionHandler interactionHandler,
        IApiSecurityStore apiSecurityStore,
        FirstRunAdminSetupService adminSetupService,
        ILogger<StartupService> logger) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Initializing application infrastructure...");

            try
            {
                await apiSecurityStore.InitializeAsync();
                await adminSetupService.InitializeAsync();

                await interactionHandler.InitializeAsync();

                await discord.RequestStartup();

                logger.LogInformation("Infrastructure initialized successfully. Bot is going online.");
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "A critical error occurred during the startup sequence.");
                throw;
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Application is shutting down. Stopping Discord client safely...");

            await discord.RequestShutdown();
        }
    }
}