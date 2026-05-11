using Client.Data;
using Client.Handlers;
using Client.Models;
using Client.Security;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Client.Services;

/// <summary>
/// </summary>
public class StartupService(
    DiscordSocketClient discordClient,
    InteractionHandler interactionHandler,
    IApiSecurityStore apiSecurityStore,
    FirstRunAdminSetupService adminSetupService,
    ZabbixService zabbixService,
    DiscordStateService stateService,
    IOptions<AppDiscordConfig> discordConfig,
    ILogger<StartupService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Initializing application infrastructure...");

        try
        {
            discordClient.Log += LogDiscordMessageAsync;

            await apiSecurityStore.InitializeAsync();
            await adminSetupService.InitializeAsync();

            await interactionHandler.InitializeAsync();

            string token = discordConfig.Value.apiToken;
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException("Discord API token is missing in the configuration.");
            }

            await discordClient.LoginAsync(TokenType.Bot, token);
            await discordClient.StartAsync();

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

        try
        {
            await discordClient.StopAsync();
            await discordClient.LogoutAsync();
            discordClient.Log -= LogDiscordMessageAsync;

            logger.LogInformation("Discord client stopped successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while stopping the Discord client.");
        }
    }

    /// <summary>
    /// </summary>
    private Task LogDiscordMessageAsync(LogMessage message)
    {
        var level = message.Severity switch
        {
            LogSeverity.Critical => LogEventLevel.Fatal,
            LogSeverity.Error => LogEventLevel.Error,
            LogSeverity.Warning => LogEventLevel.Warning,
            LogSeverity.Info => LogEventLevel.Information,
            LogSeverity.Verbose => LogEventLevel.Verbose,
            LogSeverity.Debug => LogEventLevel.Debug,
            _ => LogEventLevel.Information
        };

        var contextLogger = Log.ForContext("SourceContext", $"Discord.{message.Source ?? "General"}");
        contextLogger.Write(level, message.Exception, message.Message);

        return Task.CompletedTask;
    }
}
