using Application.Repositories;
using Application.Services.API;
using Discord;
using Discord.Interactions;
using Infrastructure.Exceptions;
using Infrastructure.Extensions;
using Infrastructure.Models.Modals;

namespace Infrastructure.Discord.SlashCommands.Commands.Api.Clients.Actions;

public sealed class ClientZabbixConnectionAction(
    IApiSecurityStore apiSecurityStore,
    IIntegrationClientRepository apiClientRepository,
    ApiClientPanelRenderer panelRenderer)
{
    public Task ShowModalAsync(DiscordInteractionView module, long clientId)
    {
        var zabbixModal = new ModalBuilder()
            .WithTitle("Update Zabbix Connection")
            .WithCustomId($"client_modal_zabbix:{clientId}")
            .AddTextInput("New Zabbix API URL", "url", TextInputStyle.Short, "https://zabbix.yourdomain.com/api_jsonrpc.php", required: true)
            .AddTextInput("New Zabbix API Token", "token", TextInputStyle.Short, "Enter new token...", required: true);

        return module.RespondWithModalInteractionAsync(zabbixModal.Build());
    }

    public async Task HandleModalAsync(DiscordInteractionView module, long clientId,
        ZabbixCredentialsModal modal)
    {
        await module.DeferInteractionAsync(ephemeral: true);

        var isValidUrl = modal.Url.IsValidHttpOrHttpsUrl();

        if (!isValidUrl) throw new Exceptions.InteractionException("The provided Zabbix API URL is not valid. Please ensure it starts with http:// or https:// and is properly formatted.");

        await apiSecurityStore.UpdateZabbixConnectionAsync(clientId, modal.Url, modal.Token);

        var client = await apiClientRepository.GetByIdAsync(clientId);
        if (client == null) throw new Exceptions.InteractionException("Client not found.");

        var components = panelRenderer.CreateManagementPanel(client, module.Context);

        await ((IModalInteraction)module.Context.Interaction).ModifyOriginalResponseAsync(msg => msg.Components = components);
        await module.FollowupInteractionAsync("Zabbix connection credentials successfully updated.", ephemeral: true);
    }
}
