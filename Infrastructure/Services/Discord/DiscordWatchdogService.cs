using Discord;
using Discord.WebSocket;
using Domain.Enums;
using Infrastructure.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Services.Discord;

/// <summary>
/// Background service responsible for monitoring Discord connection state 
/// and forcing a hard restart if the connection is dead for too long.
/// </summary>
public class DiscordWatchdogService(
    DiscordSocketClient client,
    DiscordStateService stateService,
    IOptions<AppDiscordConfig> discordConfig,
    IHttpClientFactory httpClientFactory,
    ILogger<DiscordWatchdogService> logger) : BackgroundService
{
    // Time to wait before considering the Discord client critically offline and in need of a hard reset
    private readonly TimeSpan _criticalOfflineThreshold = TimeSpan.FromMinutes(2);

    // How often to check the Discord connection health
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Discord Watchdog Service has started. Monitoring connection health...");

        // Timer for background service loop
        using var timer = new PeriodicTimer(_checkInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            if (stateService.HealthState != DiscordHealthState.Offline ||
                stateService.OfflineDuration < _criticalOfflineThreshold)
            {
                // Everything is fine or we are within the grace period.
                continue;
            }

            logger.LogWarning("Discord client has been offline for {Minutes} minutes. Attempting hard reset...", stateService.OfflineDuration.TotalMinutes);

            bool isApiReachable = await PingDiscordApiAsync(stoppingToken);

            if (!isApiReachable)
            {
                logger.LogWarning("Discord API is currently unreachable. Aborting hard reset. Will retry in the next cycle.");
                continue;
            }

            logger.LogInformation("Discord API is responding. Proceeding with hard reset sequence.");
            await PerformHardResetAsync();
        }
    }

    private async Task<bool> PingDiscordApiAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            // Preforming a simple GET request to the Discord API gateway endpoint to check if it's responsive.
            var response = await httpClient.GetAsync("https://discord.com/api/v10/gateway", cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task PerformHardResetAsync()
    {
        try
        {
            var token = discordConfig.Value.apiToken;
            if (string.IsNullOrWhiteSpace(token))
            {
                logger.LogCritical("Discord API token is missing in the configuration. Cannot restart client.");
                return;
            }

            // Shutdown sequence
            logger.LogInformation("Stopping gateway connection...");
            await client.StopAsync();

            logger.LogInformation("Logging out to clear internal state...");
            await client.LogoutAsync();

            // Safe delay to ensure all resources are cleaned up before restarting
            await Task.Delay(1000);

            // Startup sequence
            logger.LogInformation("Logging in with clean state...");
            await client.LoginAsync(TokenType.Bot, token);

            logger.LogInformation("Starting gateway connection...");
            await client.StartAsync();

            logger.LogInformation("Resuscitation protocol completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "A critical error occurred during the resuscitation protocol");
        }
    }
}