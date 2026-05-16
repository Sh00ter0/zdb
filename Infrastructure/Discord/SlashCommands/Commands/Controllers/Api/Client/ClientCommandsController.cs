using Application.Common.Constants;
using Application.Discord.Panels.Core;
using Application.Discord.Panels.Rendering;
using Application.Repositories;
using Application.Services.API;
using Application.Services.Discord;
using Application.Services.Pagination;
using Discord;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Exceptions;
using Infrastructure.Extensions;
using Infrastructure.Models.Modals;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Infrastructure.Discord.SlashCommands.Commands.Controllers.Api.Client;

public class ClientCommandsController(
    ILogger<ClientCommandsController> logger,
    IApiSecurityStore apiSecurityStore,
    IIntegrationClientRepository apiClientRepository,
    IKnownDeliveryTargetRepository targetRepository,
    IDiscordUiService discordUiService,
    IPaginationService paginationService,
    IDiscordEmoteService emoteCache,
    IPanelRegistry panelRegistry,
    IPanelRenderer panelRenderer,
    DiscordLayoutMapper layoutMapper)
{
    public async Task CreateApiClientAsync(AppInteractionContext context, string clientName, string zabbixApiUrl, string zabbixApiToken)
    {
        logger.LogInformation("Received request to create a new API client. Name: {ClientName}", clientName);
        await context.Interaction.DeferAsync(ephemeral: true);

        if (!zabbixApiUrl.IsValidHttpOrHttpsUrl())
            throw new UserVisibleException("The provided Zabbix API URL is not valid. Please ensure it starts with http:// or https:// and is properly formatted.");

        try
        {
            var createdClient = await apiSecurityStore.CreateApiClientAsync(clientName, zabbixApiUrl, zabbixApiToken);
            var bodyText = $"""
                **Client name:** `{createdClient.Name}`
                **Zabbix API URL:** `{zabbixApiUrl}`
                **API key:** `{createdClient.ApiKey}`
                
                ⚠️ **Warning!:** Copy and store this key now. It is only shown once.
                """;

            var components = discordUiService.CreateStandardContainer(header: "API key created", accentColor: null, body: bodyText);
            await context.Interaction.FollowupAsync(components: components, flags: MessageFlags.ComponentsV2, ephemeral: true);
            logger.LogInformation("Successfully created API client and generated key for: {ClientName}", createdClient.Name);
        }
        catch (InvalidOperationException ex) { throw new UserVisibleException(ex.Message); }
    }

    public async Task ManageApiClientAsync(AppInteractionContext context, IServiceProvider serviceProvider, string clientName)
    {
        await context.Interaction.DeferAsync(ephemeral: true);

        var client = await apiClientRepository.GetByNameAsync(clientName)
            ?? throw new UserVisibleException($"API Client `{clientName}` not found.");

        var panel = panelRegistry.Get("client");

        var panelContext = new ConfigPanelContext
        {
            Context = context,
            Services = serviceProvider,
            UserId = context.User.Id,
            EntityId = client.Id.ToString(),
            RawInteractionData = null
        };

        var state = await panel.BuildStateAsync(panelContext);
        var renderedPanel = await panelRenderer.RenderAsync(state);

        var finalComponents = renderedPanel.Layout != null
            ? layoutMapper.Map(renderedPanel.Layout)
            : renderedPanel.Components;

        await context.Interaction.FollowupAsync(
            text: renderedPanel.Content,
            embeds: null, // <-- Wymuszamy brak embedów
            components: finalComponents,
            ephemeral: true,
            flags: MessageFlags.ComponentsV2 // <-- Odpalamy piękny kontener V2
        );
    }
}