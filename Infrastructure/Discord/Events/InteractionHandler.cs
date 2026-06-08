using Application.Repositories;
using Application.Services.Discord;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Infrastructure.Discord.SlashCommands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Discord.Events;

public class InteractionHandler(
    DiscordSocketClient client,
    InteractionService handler,
    IServiceProvider services,
    ILogger<InteractionHandler> logger,
    IDiscordEmoteService emoteCache,
    InteractionErrorResponder errorResponder)
{
    private bool _commandsRegistered = false;

    public async Task InitializeAsync()
    {
        client.Ready += ReadyAsync;
        handler.Log += LogAsync;
        client.InteractionCreated += HandleInteraction;
        handler.InteractionExecuted += HandleInteractionExecute;
    }

    private Task LogAsync(LogMessage log)
    {
        var logLevel = log.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Trace,
            LogSeverity.Debug => LogLevel.Debug,
            _ => LogLevel.Information
        };

        logger.Log(logLevel, log.Exception, "InteractionService: {Message}", log.Message);
        return Task.CompletedTask;
    }

    private Task ReadyAsync()
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (!_commandsRegistered)
                {
                    logger.LogInformation("Discord client is ready. Verifying modules and registering commands...");

                    await emoteCache.SynchronizeEmotesAsync();

                    await handler.AddModulesAsync(typeof(InteractionHandler).Assembly, services);
                    logger.LogDebug("Successfully loaded all interaction modules into the service.");

                    await handler.RegisterCommandsGloballyAsync(deleteMissing: true);

                    _commandsRegistered = true;
                    logger.LogInformation("Application commands registered successfully.");
                }
                else
                {
                    logger.LogInformation("Reconnected to Discord. Commands are already registered, skipping API sync to prevent rate limits.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during ReadyAsync background processing.");
            }
        });

        return Task.CompletedTask;
    }

    private Task HandleInteraction(SocketInteraction interaction)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = services.CreateScope();
                var adminRepo = scope.ServiceProvider.GetRequiredService<ISystemAdministratorRepository>();

                var adminEntity = await adminRepo.GetByDiscordIdAsync(interaction.User.Id);
                var context = new AppInteractionContext(client, interaction, adminEntity);

                await handler.ExecuteCommandAsync(context, services);
            }
            catch (Exception ex)
            {
                await errorResponder.RespondToRoutingExceptionAsync(interaction, ex);
            }
        });

        return Task.CompletedTask;
    }

    private async Task HandleInteractionExecute(ICommandInfo commandInfo, IInteractionContext context, IResult result)
    {
        if (result.IsSuccess)
        {
            logger.LogInformation("Successfully executed interaction '{CommandName}' for user {UserId}", commandInfo?.Name ?? "Unknown", context.User.Id);
            return;
        }

        await errorResponder.RespondToFailedExecutionAsync(commandInfo, context, result);
    }
}
