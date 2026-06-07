using Application.Repositories;
using Application.Services.API;
using Application.Services.Discord;
using Discord;
using Discord.Interactions;
using Domain.Constants;
using Domain.Enums;
using Infrastructure.Discord.SlashCommands.Commands.Api.Clients.Actions;
using Infrastructure.Exceptions;
using Infrastructure.Extensions;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Discord.SlashCommands.Commands.Api.Clients;

public sealed class ApiClientCommandsBackend(
    ILogger<ApiClientCommandsBackend> logger,
    IApiSecurityStore apiSecurityStore,
    IIntegrationClientRepository apiClientRepository,
    IDiscordUiService discordUiService,
    ApiClientPanelRenderer panelRenderer,
    ClientChangeNameAction changeNameAction,
    ClientStatusAction statusAction,
    ClientZabbixConnectionAction zabbixConnectionAction,
    ClientTargetsListAction targetsListAction,
    ClientRenewApiKeyAction renewApiKeyAction,
    ClientRemoveAction removeAction,
    ClientCancelAction cancelAction)
{
    public async Task CreateApiClientAsync(DiscordInteractionView module, string clientName,
        string zabbixApiUrl, string zabbixApiToken)
    {
        logger.LogInformation("Received request to create a new API client. Name: {ClientName}", clientName);
        await module.DeferInteractionAsync(ephemeral: true);

        try
        {
            var isValidUrl = zabbixApiUrl.IsValidHttpOrHttpsUrl();
            if (!isValidUrl) throw new UserVisibleException("The provided Zabbix API URL is not valid. Please ensure it starts with http:// or https:// and is properly formatted.");

            var createdClient = await apiSecurityStore.CreateApiClientAsync(clientName, zabbixApiUrl, zabbixApiToken);

            var bodyText = $"""
                            **Client name:** `{createdClient.Name}`
                            **Zabbix API URL:** `{zabbixApiUrl}`
                            **API key:** `{createdClient.ApiKey}`
                            
                            ⚠️ **Warning!:** Copy and store this key now. It is only shown once.
                            """;

            var components = discordUiService.CreateStandardContainer(header: "API key created", accentColor: null, body: bodyText);

            await module.FollowupInteractionAsync(components: components, flags: MessageFlags.ComponentsV2, ephemeral: true);
            logger.LogInformation("Successfully created API client and generated key for: {ClientName}", createdClient.Name);
        }
        catch (InvalidOperationException ex)
        {
            throw new UserVisibleException(ex.Message);
        }
    }

    public async Task ManageApiClientAsync(DiscordInteractionView module, string clientName)
    {
        var client = await apiClientRepository.GetByNameAsync(clientName);
        if (client is null) throw new UserVisibleException($"API Client `{clientName}` not found.");

        var components = panelRenderer.CreateManagementPanel(client, module.Context);

        await module.RespondInteractionAsync(components: components, ephemeral: true, flags: MessageFlags.ComponentsV2);
    }

    public async Task HandleClientActionSelectAsync(DiscordInteractionView module, long clientId,
        string[] selectedValues)
    {
        var action = Enum.Parse<ApiClientModifyingAction>(selectedValues[0]);
        var client = await apiClientRepository.GetByIdAsync(clientId);
        if (client == null) throw new UserVisibleException("Client not found.");

        switch (action)
        {
            case ApiClientModifyingAction.ChangeName:
                await changeNameAction.ShowModalAsync(module, clientId);
                break;

            case ApiClientModifyingAction.EnableOrDisableClient:
                await statusAction.ShowPanelAsync(module, client);
                break;

            case ApiClientModifyingAction.RenewZabbixConnection:
                await zabbixConnectionAction.ShowModalAsync(module, clientId);
                break;

            case ApiClientModifyingAction.DisplayRelatedTargets:
                await targetsListAction.ShowAsync(module, client);
                break;

            case ApiClientModifyingAction.RenewApiKey:
                await renewApiKeyAction.ShowConfirmationAsync(module, client);
                break;

            case ApiClientModifyingAction.Remove:
                await removeAction.ShowConfirmationAsync(module, client);
                break;
        }
    }

    public Task HandleClientCancelAsync(DiscordInteractionView module, long clientId)
    {
        return cancelAction.ExecuteAsync(module, clientId);
    }

    public Task HandleClientRenameModalAsync(DiscordInteractionView module, long clientId,
        Infrastructure.Models.Modals.ClientActionModal modal)
    {
        return changeNameAction.HandleModalAsync(module, clientId, modal);
    }

    public Task HandleClientStatusSelectAsync(DiscordInteractionView module, long clientId,
        string[] selectedValues)
    {
        return statusAction.HandleSelectAsync(module, clientId, selectedValues);
    }

    public Task HandleClientZabbixModalAsync(DiscordInteractionView module, long clientId,
        Infrastructure.Models.Modals.ZabbixCredentialsModal modal)
    {
        return zabbixConnectionAction.HandleModalAsync(module, clientId, modal);
    }

    public Task HandleClientRenewKeyConfirmAsync(DiscordInteractionView module, long clientId)
    {
        return renewApiKeyAction.ConfirmAsync(module, clientId);
    }

    public Task HandleClientRemoveConfirmAsync(DiscordInteractionView module, long clientId)
    {
        return removeAction.ConfirmAsync(module, clientId);
    }
}
