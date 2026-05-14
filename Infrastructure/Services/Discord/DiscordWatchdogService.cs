using Domain.Enums;
using Infrastructure.Mediators;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Discord;

public class DiscordWatchdogService(DiscordStartupService discordStartup,
    IHttpClientFactory httpClientFactory,
    ILogger<DiscordWatchdogService> logger,
    DiscordStateMediator mediator
    ) : IDisposable, IHostedService
{
    private readonly TimeSpan _criticalOfflineThreshold = TimeSpan.FromMinutes(2);

    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

    private int _failedResetAttempts = 0;

    private CancellationTokenSource CancelationToken = new();

    private async void OnStateChanged(DiscordHealthState newState)
    {
        switch (newState)
        {
            case DiscordHealthState.Healthy:
                ResetWatcher();
                break;
            case DiscordHealthState.Degraded:
                logger.LogWarning("Discord connection state changed to Degraded. Monitoring closely...");
                await StartWatcher();
                break;
            case DiscordHealthState.Offline:
                logger.LogError("Discord connection state changed to Offline. Will attempt recovery if condition persists.");
                await StartWatcher();
                break;
        }
    }

    private void ResetWatcher()
    {
        logger.LogInformation("Discord connection state changed to Healthy.");

        _failedResetAttempts = 0;
        CancelationToken.Cancel();
    }

    private async Task StartWatcher()
    {
        if (!CancelationToken.Token.IsCancellationRequested) return;

        CancelationToken = new();

        await Task.Delay(_criticalOfflineThreshold, CancelationToken.Token);

        while (!CancelationToken.Token.IsCancellationRequested)
        {
            await Task.Delay(_checkInterval, CancelationToken.Token);
            if (CancelationToken.Token.IsCancellationRequested) return;

            bool isApiReachable = await PingDiscordApiAsync();
            if (isApiReachable)
            {
                logger.LogWarning("Discord API is currently unreachable. Aborting client hard reset. Will retry in the next cycle.");
                continue;
            }

            await PerformHardResetAsync();
            _failedResetAttempts++;
            if (_failedResetAttempts >= 3)
            {
                logger.LogCritical("Failed to restore Discord connection after {Attempts} consecutive hard reset attempts. Manual intervention may be required.", _failedResetAttempts);
            }
            else
            {
                logger.LogWarning("Hard reset sequence finished, but client failed to connect. Attempt {Attempt}/3.", _failedResetAttempts);
            }
        }
    }

    private async Task<bool> PingDiscordApiAsync()
    {
        try
        {
            using var httpClient = httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var response = await httpClient.GetAsync("https://discord.com/api/v10/gateway", CancelationToken.Token);

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task PerformHardResetAsync()
    {
        await discordStartup.RequestShutdown();

        await Task.Delay(1000);

        await discordStartup.RequestStartup();
    }

    public void Dispose()
    {
        mediator.StateChanged -= OnStateChanged;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        mediator.StateChanged += OnStateChanged;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Dispose();
    }
}