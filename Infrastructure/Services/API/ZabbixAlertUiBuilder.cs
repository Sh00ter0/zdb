using Application.Common.Zabbix;
using Application.Services.Discord;
using Discord;
using Discord.WebSocket;
using Domain.Constants;
using Infrastructure.Views.Components;
using Infrastructure.Views.Layouts;

namespace Infrastructure.Services.API;

public sealed class ZabbixAlertUiBuilder(DiscordSocketClient client, IDiscordEmoteService emoteCache)
{
    public MessageComponent CreateAlertContainer(ZabbixPayload payload, bool isDm, long apiClientId)
    {
        Color containerColor;
        if (payload.EventSource is 1 or 2) containerColor = AppColors.Info;
        else
        {
            var isResolved = payload.EventValue == 0;
            if (isResolved) containerColor = AppColors.Success;
            else
            {
                containerColor = payload.Severity switch
                {
                    1 => AppColors.SeverityInformation,
                    2 => AppColors.SeverityWarning,
                    3 => AppColors.SeverityAverage,
                    4 => AppColors.SeverityHigh,
                    5 => AppColors.SeverityDisaster,
                    _ => AppColors.SeverityNotClassified
                };
            }
        }

        var safeMessage = payload.Message.Length >= 3900
            ? string.Concat(payload.Message.AsSpan(0, 3900), "…")
            : payload.Message;

        var layout = new StandardLayout(emoteCache, client);
        layout.Create(payload.Subject)
            .WithAccentColor(containerColor)
            .AddSection(
                new TextSection(safeMessage)
            );

        if (!isDm || payload.ControlMenu != 1 || payload.EventValue == 0 || payload.EventSource == 1 ||
            payload.EventSource == 2) return layout.Build();

        var pulseIcon = emoteCache.GetEmote("UI_ICON_PULSE");
        var actionButton = new ButtonBuilder()
            .WithCustomId($"btn_manage:{apiClientId}:{payload.EventId}")
            .WithLabel("Take action")
            .WithStyle(ButtonStyle.Primary)
            .WithEmote(pulseIcon);

        layout.AddSection(
            new ActionSection([actionButton])
        );

        return layout.Build();
    }
}
