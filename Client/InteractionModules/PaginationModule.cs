using Application.Services.API;
using Application.Services.Discord;
using Application.Services.Pagination;
using Client.Contexts;
using Client.Models;
using Client.Security;
using Client.Services;
using Discord.Interactions;
using Discord.WebSocket;

namespace Client.InteractionModules
{
    public class PaginationModule : InteractionModuleBase<AppInteractionContext>
    {
        private readonly ILogger<PaginationModule> _logger;
        private readonly IApiSecurityStore _apiSecurityStore;
        private readonly ZabbixService _zabbixService;
        private readonly IDiscordUiService _discordUiService;
        private readonly IPaginationService _paginationService;

        public PaginationModule(
            ZabbixService zabbixService,
            ILogger<PaginationModule> logger,
            IApiSecurityStore apiSecurityStore,
            IDiscordUiService discordUiService,
            IPaginationService paginationService)
        {
            _zabbixService = zabbixService;
            _logger = logger;
            _apiSecurityStore = apiSecurityStore;
            _paginationService = paginationService;
            _discordUiService = discordUiService;
        }

        [ComponentInteraction("nav:*:*:*", ignoreGroupNames: true)]
        public async Task HandlePaginationButtonAsync(string sessionId, int targetPage, string actionType)
        {
            var sessionData = _paginationService.GetSessionData(sessionId);

            if (sessionData == null || sessionData.Pages.Count == 0)
            {
                throw new UserVisibleException("The session for this message has expired. Please run the command again.");
            }

            if (targetPage < 1 || targetPage > sessionData.Pages.Count)
            {
                await DeferAsync();
                return;
            }

            var pageText = sessionData.Pages[targetPage - 1];

            var components = _discordUiService.CreatePaginatedContainer(
                header: sessionData.Header,
                pageText: pageText,
                currentPage: targetPage,
                totalPages: sessionData.Pages.Count,
                sessionId: sessionId,
                customActionBtn: sessionData.CustomButton
            );

            if (Context.Interaction is SocketMessageComponent componentInteraction)
            {
                await componentInteraction.UpdateAsync(msg =>
                {
                    msg.Components = components;
                });
            }
        }
    }
}
