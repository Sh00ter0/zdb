using Application.Common.Zabbix;
using Discord;
using Domain.Entities;

namespace Application.Services.Discord
{
    public interface IDiscordUiService
    {
        MessageComponent CreateStandardContainer(string header, string body, Color? accentColor = null, string? footerNote = null);
        Modal CreateConfirmationModal(string customId, string title, string inputLabel, string placeholder, int maxLength);
        MessageComponent CreatePaginatedContainer(string header, string pageText, int currentPage, int totalPages, string sessionId, Color? accentColor = null, ButtonBuilder? customActionBtn = null);
        MessageComponent CreateZabbixAlertContainer(ZabbixPayload payload, bool isDm, long apiClientId);

        SelectMenuBuilder GetZabbixAckMenuBuilder(string customId, bool currentState);
        SelectMenuBuilder GetZabbixSeverityMenuBuilder(string customId, int currentSeverity);
        MessageComponent CreateZabbixManagementPanel(string eventId, SelectMenuBuilder ackMenu, SelectMenuBuilder sevMenu, ButtonBuilder commentBtn);
        Modal CreateZabbixCommentModal(string customId);

        MessageComponent CreateTargetOverviewContainer(string clientName, KnownDeliveryTargets target, Action<ContainerBuilder> appendComponents = null!);
        SelectMenuBuilder GetTargetManagementMenuBuilder(string customId, List<string> userPermissions);
        SelectMenuBuilder GetCrosspostSelectMenuBuilder(string customId, bool currentState);

        MessageComponent CreateApiClientOverviewContainer(IntegrationClients client, Action<ContainerBuilder> appendComponents = null!);
        SelectMenuBuilder GetApiClientManagementMenuBuilder(string customId, List<string> userPermissions);
        SelectMenuBuilder GetClientStatusSelectMenuBuilder(string customId, bool currentState);

        MessageComponent CreateAdminOverviewContainer(SystemAdministrators adminEntity, IUser discordUser, Action<ContainerBuilder> appendComponents = null!);
        SelectMenuBuilder GetAdminActionMenuBuilder(string customId, SystemAdministrators targetAdmin, SystemAdministrators requestingAdmin);
        SelectMenuBuilder GetSystemRoleMenuBuilder(string customId, int currentRoleId, List<SystemRoles> assignableRoles);
        SelectMenuBuilder GetAdminStatusMenuBuilder(string customId, bool currentState);
    }
}
