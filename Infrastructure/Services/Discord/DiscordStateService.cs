using Discord;
using Discord.WebSocket;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Discord;

public class DiscordStateService
{
    private readonly DiscordSocketClient _client;
    private readonly ILogger<DiscordStateService> _logger;

    private DateTimeOffset? _disconnectedAt;
    private readonly TimeSpan _gracePeriod = TimeSpan.FromSeconds(30);

    public DiscordStateService(DiscordSocketClient client, ILogger<DiscordStateService> logger)
    {
        _client = client;
        _logger = logger;

        _client.Ready += OnReady;
        _client.Disconnected += OnDisconnected;
        _client.Connected += OnConnected;
        _client.LoggedOut += OnLoggedOut;
    }

    public DiscordHealthState HealthState
    {
        get
        {
            if (_client.LoginState == LoginState.LoggedIn &&
                _client.ConnectionState == ConnectionState.Connected)
            {
                return DiscordHealthState.Healthy;
            }

            if (_disconnectedAt.HasValue &&
                DateTimeOffset.UtcNow - _disconnectedAt.Value < _gracePeriod)
            {
                return DiscordHealthState.Degraded;
            }

            return DiscordHealthState.Offline;
        }
    }

    public bool IsOperational =>
        HealthState is DiscordHealthState.Healthy or DiscordHealthState.Degraded;

    public TimeSpan OfflineDuration => _disconnectedAt.HasValue
        ? DateTimeOffset.UtcNow - _disconnectedAt.Value
        : TimeSpan.Zero;

    private Task OnReady()
    {
        _disconnectedAt = null;
        _logger.LogInformation("Discord gateway is ready and operational");
        return Task.CompletedTask;
    }

    private Task OnConnected()
    {
        _disconnectedAt = null;
        _logger.LogInformation("Discord gateway connection established");
        return Task.CompletedTask;
    }

    private Task OnDisconnected(Exception ex)
    {
        _disconnectedAt ??= DateTimeOffset.UtcNow;

        _logger.LogWarning("Discord gateway disconnected. Reason: {Reason}", ex?.Message ?? "Unknown");
        return Task.CompletedTask;
    }

    private Task OnLoggedOut()
    {
        _disconnectedAt ??= DateTimeOffset.UtcNow;

        _logger.LogWarning("Discord client logged out");
        return Task.CompletedTask;
    }
}