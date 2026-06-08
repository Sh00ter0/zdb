using Application.Services.Discord;
using Discord;
using Discord.Interactions;
using Domain.Constants;
using Domain.Enums;
using Infrastructure.Exceptions;
using Microsoft.Extensions.Logging;
using AppInteractionException = Infrastructure.Exceptions.InteractionException;
using DiscordInteractionException = Discord.Interactions.InteractionException;

namespace Infrastructure.Discord.Events;

public sealed class InteractionErrorResponder(
    IDiscordUiService discordUiService,
    ILogger<InteractionErrorResponder> logger)
{
    public async Task RespondToRoutingExceptionAsync(IDiscordInteraction interaction, Exception exception)
    {
        var referenceId = CreateReferenceId();
        logger.LogCritical(exception, "A critical infrastructure error occurred while trying to route the interaction. ReferenceId: {ReferenceId}", referenceId);

        await SendUnexpectedErrorAsync(interaction, referenceId);
    }

    public async Task RespondToFailedExecutionAsync(ICommandInfo? commandInfo, IInteractionContext context, IResult result)
    {
        switch (result.Error)
        {
            case InteractionCommandError.UnmetPrecondition:
                if (IsCriticalPreconditionFailure(result.ErrorReason))
                {
                    await RespondToUnexpectedResultAsync(commandInfo, context, result, null);
                    return;
                }

                await RespondToInteractionExceptionAsync(
                    commandInfo,
                    context,
                    new AppInteractionException(
                        result.ErrorReason ?? "Access denied.",
                        InteractionExceptionLevel.Warn,
                        logCopy: true,
                        header: "Access Denied"));
                return;

            case InteractionCommandError.UnknownCommand:
                await RespondToInteractionExceptionAsync(
                    commandInfo,
                    context,
                    new AppInteractionException(
                        "The requested command is unknown or no longer exists.",
                        InteractionExceptionLevel.Warn,
                        logCopy: true,
                        header: "Unknown Interaction"));
                return;

            case InteractionCommandError.BadArgs:
                await RespondToInteractionExceptionAsync(
                    commandInfo,
                    context,
                    new AppInteractionException(
                        "Invalid parameters were provided. Please check your inputs.",
                        InteractionExceptionLevel.Warn,
                        header: "Invalid Parameters"));
                return;

            case InteractionCommandError.Exception:
                var originalException = UnwrapExecutionException(result);
                if (originalException is AppInteractionException interactionException)
                {
                    await RespondToInteractionExceptionAsync(commandInfo, context, interactionException);
                    return;
                }

                await RespondToUnexpectedResultAsync(commandInfo, context, result, originalException);
                return;

            default:
                await RespondToUnexpectedResultAsync(commandInfo, context, result, null);
                return;
        }
    }

    private async Task RespondToInteractionExceptionAsync(ICommandInfo? commandInfo, IInteractionContext context, AppInteractionException exception)
    {
        if (exception.LogCopy)
        {
            LogInteractionException(commandInfo, context.User.Id, exception);
        }

        var components = discordUiService.CreateStandardContainer(
            header: exception.Header,
            body: exception.Message,
            accentColor: GetColor(exception.Level));

        await SendAsync(context.Interaction, components);
    }

    private async Task RespondToUnexpectedResultAsync(ICommandInfo? commandInfo, IInteractionContext context, IResult result, Exception? exception)
    {
        var referenceId = CreateReferenceId();

        logger.LogError(exception,
            "Unexpected interaction error. ReferenceId: {ReferenceId}. Command: {CommandName}. UserId: {UserId}. Error: {Error}. Details: {Details}",
            referenceId,
            commandInfo?.Name ?? "Unknown",
            context.User.Id,
            result.Error,
            result.ErrorReason);

        await SendUnexpectedErrorAsync(context.Interaction, referenceId);
    }

    private async Task SendUnexpectedErrorAsync(IDiscordInteraction interaction, string referenceId)
    {
        var components = discordUiService.CreateStandardContainer(
            header: "Application Error",
            body: "An unexpected application error occurred while processing your interaction. The error has been registered. Contact an administrator and provide the reference code below.",
            accentColor: AppColors.Error,
            footerNote: $"Reference: `{referenceId}`");

        await SendAsync(interaction, components);
    }

    private async Task SendAsync(IDiscordInteraction interaction, MessageComponent components)
    {
        try
        {
            if (!interaction.HasResponded)
            {
                await interaction.RespondAsync(components: components, flags: MessageFlags.ComponentsV2, ephemeral: true);
            }
            else
            {
                await interaction.FollowupAsync(components: components, flags: MessageFlags.ComponentsV2, ephemeral: true);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send interaction error feedback to the user.");
        }
    }

    private void LogInteractionException(ICommandInfo? commandInfo, ulong userId, AppInteractionException exception)
    {
        var logLevel = exception.Level switch
        {
            InteractionExceptionLevel.Info => LogLevel.Information,
            InteractionExceptionLevel.Warn => LogLevel.Warning,
            InteractionExceptionLevel.Crit => LogLevel.Critical,
            _ => LogLevel.Warning
        };

        logger.Log(logLevel, exception,
            "Expected interaction exception. Command: {CommandName}. UserId: {UserId}. Level: {Level}. Message: {Message}",
            commandInfo?.Name ?? "Unknown",
            userId,
            exception.Level,
            exception.Message);
    }

    private static Exception? UnwrapExecutionException(IResult result)
    {
        if (result is not ExecuteResult executeResult)
        {
            return null;
        }

        return executeResult.Exception is DiscordInteractionException interactionException
            ? interactionException.InnerException
            : executeResult.Exception;
    }

    private static bool IsCriticalPreconditionFailure(string? errorReason)
    {
        return errorReason?.StartsWith("Critical Error:", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static Color GetColor(InteractionExceptionLevel level)
    {
        return level switch
        {
            InteractionExceptionLevel.Info => AppColors.Info,
            InteractionExceptionLevel.Warn => AppColors.Warning,
            InteractionExceptionLevel.Crit => AppColors.Error,
            _ => AppColors.Warning
        };
    }

    private static string CreateReferenceId()
    {
        return $"{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
    }
}
