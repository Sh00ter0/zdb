using Discord;
using Discord.WebSocket;
using Domain.Enums;
using Infrastructure.Mediators;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Discord;

public class DiscordStateService
{
    private readonly DiscordSocketClient _client;
    private readonly ILogger<DiscordStateService> _logger;
    private readonly DiscordStateMediator _mediator;
    public bool IsOperational => _mediator.HealthState == DiscordHealthState.Healthy;
    public DiscordHealthState HealthState => _mediator.HealthState;

    public DiscordStateService(DiscordSocketClient client, ILogger<DiscordStateService> logger, DiscordStateMediator mediator)
    {
        _client = client;
        _logger = logger;
        _mediator = mediator;

        _client.Ready += OnReady;
        _client.Disconnected += OnDisconnected;
        _client.Connected += OnConnected;
        _client.LoggedOut += OnLoggedOut;
    }

    private Task OnReady()
    {
        _logger.LogInformation("Discord gateway is ready and operational");
        _mediator.ChangeState(DiscordHealthState.Healthy);

        return Task.CompletedTask;
    }

    private Task OnConnected()
    {
        _logger.LogInformation("Discord gateway connection established");
        _mediator.ChangeState(DiscordHealthState.Healthy);

        return Task.CompletedTask;
    }

    private Task OnDisconnected(Exception ex)
    {
        _logger.LogWarning("Discord gateway disconnected. Reason: {Reason}", ex?.Message ?? "Unknown");
        _mediator.ChangeState(DiscordHealthState.Offline);

        return Task.CompletedTask;
    }

    private Task OnLoggedOut()
    {
        _logger.LogWarning("Discord client logged out");
        _mediator.ChangeState(DiscordHealthState.Offline);

        return Task.CompletedTask;
    }
}