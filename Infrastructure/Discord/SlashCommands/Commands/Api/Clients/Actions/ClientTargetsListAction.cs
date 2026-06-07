using Application.Repositories;
using Application.Services.Discord;
using Application.Services.Pagination;
using Discord;
using Discord.Interactions;
using Domain.Entities;
using Infrastructure.Exceptions;
using Infrastructure.Extensions;

namespace Infrastructure.Discord.SlashCommands.Commands.Api.Clients.Actions;

public sealed class ClientTargetsListAction(
    IKnownDeliveryTargetRepository targetRepository,
    IPaginationService paginationService,
    IDiscordUiService discordUiService,
    IDiscordEmoteService emoteCache,
    ApiClientUiBuilder uiBuilder)
{
    public async Task ShowAsync(DiscordInteractionView module, IntegrationClients client)
    {
        await module.DeferInteractionAsync(ephemeral: true);

        var targetEntities = await targetRepository.GetAllByClientIdAsync(client.Id);
        var undoEmote = emoteCache.GetEmote("UI_ICON_UNDO");

        if (targetEntities.Count == 0)
        {
            var emptyContainer = uiBuilder.CreateOverviewContainer(client, cb =>
            {
                cb.WithTextDisplay("📝 **This client currently has no authorized targets.**");
                cb.WithActionRow(row => row.AddComponent(new ButtonBuilder().WithCustomId($"client_btn_cancel:{client.Id}").WithLabel("Return").WithStyle(ButtonStyle.Secondary).WithEmote(undoEmote)));
            });
            await ((IComponentInteraction)module.Context.Interaction).ModifyOriginalResponseAsync(msg => msg.Components = emptyContainer);
            return;
        }

        var items = new List<string>();
        foreach (var target in targetEntities)
        {
            var discordTimestamp = $"<t:{((DateTimeOffset)target.CreatedAtUtc).ToUnixTimeSeconds()}:F>";
            var bodyText = $"`{target.Name}`\n-# ├ **ID:** `{target.TargetId}`\n-# ├ **Type:** `{target.ChannelType.GetDisplayName()}`\n-# └ **Added:** {discordTimestamp}";
            items.Add(bodyText);
        }

        var returnButton = new ButtonBuilder()
            .WithCustomId($"client_btn_cancel:{client.Id}")
            .WithLabel("Return")
            .WithStyle(ButtonStyle.Secondary)
            .WithEmote(undoEmote);

        var headerText = $"Targets for: {client.Name}\n-# Total targets: {targetEntities.Count}";

        var sessionId = paginationService.CreatePaginationSession(
            header: headerText,
            items: items,
            charsPerPage: 1000,
            separator: "\n\n",
            customButton: returnButton
        );

        var sessionData = paginationService.GetSessionData(sessionId);

        if (sessionData == null || sessionData.Pages.Count == 0)
        {
            throw new UserVisibleException("Failed to generate target list.");
        }

        var listComponents = discordUiService.CreatePaginatedContainer(
            header: sessionData.Header,
            pageText: sessionData.Pages[0],
            currentPage: 1,
            totalPages: sessionData.Pages.Count,
            sessionId: sessionId,
            customActionBtn: sessionData.CustomButton
        );

        await ((IComponentInteraction)module.Context.Interaction).ModifyOriginalResponseAsync(msg => msg.Components = listComponents);
    }
}
