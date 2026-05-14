using Discord;
using Discord.WebSocket;
using Infrastructure.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;

namespace Infrastructure.Services.Discord
{
    public class DiscordStartupService(
        DiscordSocketClient client,
        IOptions<AppDiscordConfig> config,
        ILogger<DiscordStartupService> logger)
    {
        public async Task RequestShutdown()
        {
            try
            {
                await client.StopAsync();
                await client.LogoutAsync();
                client.Log -= LogDiscordMessageAsync;

                logger.LogInformation("Discord client stopped successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while stopping the Discord client.");
            }
        }

        public async Task RequestStartup()
        {
            try
            {
                client.Log += LogDiscordMessageAsync;

                string token = config.Value.apiToken;
                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Discord API token is missing in the configuration.");
                }

                await client.LoginAsync(TokenType.Bot, token);
                await client.StartAsync();
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "A critical error occurred during the startup sequence.");
                throw;
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
}
