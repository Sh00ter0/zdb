using Application.Repositories;
using Application.Services.Discord;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Domain.Constants;
using Infrastructure.Discord.SlashCommands;
using Infrastructure.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Infrastructure.Discord.Events;

public class InteractionHandler(
    DiscordSocketClient client,
    InteractionService handler,
    IServiceProvider services,
    IDiscordUiService discordUiService,
    ILogger<InteractionHandler> logger,
    IDiscordEmoteService emoteCache)
{
    private const string ErrorIdPrefix = "SYS";

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
                var errorId = CreateErrorId();
                logger.LogCritical(ex, "A critical infrastructure error occurred while trying to route the interaction. ErrorId: {ErrorId}", errorId);

                if (!interaction.HasResponded)
                {
                    try
                    {
                        var components = discordUiService.CreateStandardContainer(
                            header: "Critical System Error",
                            body: "A critical internal error occurred while processing your request.",
                            accentColor: new Color(AppColors.Error),
                            footerNote: $"Reference: `{errorId}`"
                        );

                        await interaction.RespondAsync(components: components, flags: MessageFlags.ComponentsV2, ephemeral: true);
                    }
                    catch { }
                }
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

        // --- DODAJ TO ZABEZPIECZENIE ---
        if (context.Interaction is IAutocompleteInteraction autocomplete)
        {
            if (!autocomplete.HasResponded)
                await autocomplete.RespondAsync(Enumerable.Empty<AutocompleteResult>());

            logger.LogWarning("Autocomplete failed for {User}. Reason: {Reason}", context.User.Username, result.ErrorReason);
            return;
        }
        // -------------------------------

        string errorHeader = "Error";
        string errorMessage = "An unexpected error occurred.";
        string? errorId = null;
        Color containerColor = AppColors.Error;

        switch (result.Error)
        {
            case InteractionCommandError.UnmetPrecondition:
                errorHeader = "Access Denied";
                errorMessage = result.ErrorReason ?? "Access denied.";
                containerColor = AppColors.Warning;
                logger.LogWarning("Precondition failed for interaction '{CommandName}' by user {UserId}. Reason: {Reason}", commandInfo?.Name, context.User.Id, errorMessage);
                break;

            case InteractionCommandError.UnknownCommand:
                errorMessage = "The requested command is unknown or no longer exists.";
                logger.LogWarning("User {UserId} tried to execute an unknown command.", context.User.Id);
                break;

            case InteractionCommandError.BadArgs:
                errorMessage = "Invalid parameters were provided. Please check your inputs.";
                containerColor = AppColors.Warning;
                logger.LogWarning("Bad arguments provided for interaction '{CommandName}' by user {UserId}.", commandInfo?.Name, context.User.Id);
                break;

            case InteractionCommandError.Exception:
                Exception? originalException = null;
                if (result is ExecuteResult execResult)
                {
                    originalException = execResult.Exception;
                    if (originalException is InteractionException interactionEx)
                    {
                        originalException = interactionEx.InnerException;
                    }
                }

                if (originalException is UserVisibleException userEx)
                {
                    errorHeader = "Action Failed";
                    errorMessage = userEx.Message;
                    containerColor = AppColors.Warning;

                    logger.LogInformation("User-visible exception during '{CommandName}' for user {UserId}. Message: {Message}",
                        commandInfo?.Name ?? "Unknown", context.User.Id, errorMessage);
                }
                else
                {
                    errorId = CreateErrorId();
                    errorMessage = "An internal application error occurred while executing the command. The administrator has been notified.";

                    logger.LogError(originalException, "Exception thrown during execution of '{CommandName}' for user {UserId}. ErrorId: {ErrorId}. Details: {Details}",
                        commandInfo?.Name ?? "Unknown", context.User.Id, errorId, result.ErrorReason);
                }
                break;

            case InteractionCommandError.Unsuccessful:
                errorId = CreateErrorId();
                errorMessage = "The command execution was unsuccessful.";
                logger.LogError("Command '{CommandName}' execution was unsuccessful for user {UserId}. ErrorId: {ErrorId}. Details: {Details}",
                    commandInfo?.Name, context.User.Id, errorId, result.ErrorReason);
                break;

            default:
                errorMessage = result.ErrorReason ?? errorMessage;
                logger.LogWarning("Unhandled error type {ErrorType} for interaction '{CommandName}' by user {UserId}. Reason: {Reason}",
                    result.Error, commandInfo?.Name, context.User.Id, errorMessage);
                break;
        }

        var responseComponents = discordUiService.CreateStandardContainer(
            header: errorHeader,
            body: errorMessage,
            accentColor: containerColor,
            footerNote: errorId != null ? $"Reference: `{errorId}`" : null
        );

        try
        {
            if (!context.Interaction.HasResponded)
            {
                await context.Interaction.RespondAsync(components: responseComponents, flags: MessageFlags.ComponentsV2, ephemeral: true);
            }
            else
            {
                await context.Interaction.FollowupAsync(components: responseComponents, flags: MessageFlags.ComponentsV2, ephemeral: true);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send the error feedback message to the user.");
        }
    }

    private static string CreateErrorId()
    {
        return $"{ErrorIdPrefix}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
    }
}