using Client.Data;
using Client.Data.Repositories;
using Client.Enums;
using Client.Models;
using Client.Services;
using Discord;
using Discord.WebSocket;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Client.Controllers
{
    [ApiController]
    [Authorize(Policy = "ZabbixIngress")]
    [EnableRateLimiting("zabbix-api")]
    [Route("api/[controller]")]
    public class ZabbixController : ControllerBase
    {
        private const string ErrorIdPrefix = "DZB";

        private readonly DiscordSocketClient _client;
        private readonly IntegrationClientRepository _clientRepository;
        private readonly KnownDeliveryTargetRepository _targetRepository;
        private readonly DiscordStateService _stateService;
        private readonly IDiscordTargetSyncService _targetSyncService;
        private readonly IDiscordUiService _discordUiService;
        private readonly ILogger<ZabbixController> _logger;

        public ZabbixController(
            DiscordSocketClient client,
            IntegrationClientRepository clientRepository,
            KnownDeliveryTargetRepository targetRepository,
            DiscordStateService stateService,
            IDiscordTargetSyncService targetSyncService,
            IDiscordUiService discordUiService,
            ILogger<ZabbixController> logger)
        {
            _client = client;
            _clientRepository = clientRepository;
            _targetRepository = targetRepository;
            _stateService = stateService;
            _targetSyncService = targetSyncService;
            _discordUiService = discordUiService;
            _logger = logger;
        }

        [HttpPost("{targetDiscordId}")]
        [RequestSizeLimit(1024 * 1024)]
        public async Task<IActionResult> ReceiveAlert(ulong targetDiscordId, [FromBody] ZabbixPayload payload)
        {
            _logger.LogInformation("Received Zabbix alert payload for Discord target {TargetId}. Event ID: {EventId}", targetDiscordId, payload.EventId);

            if (!_stateService.IsReady)
            {
                _logger.LogWarning("Rejecting Zabbix alert {EventId}: The Discord client is not connected or ready.", payload.EventId);
                return StatusCode(503, new { error = "Discord client not ready" });
            }

            var apiClientIdClaim = User.FindFirstValue("ApiClientId");
            if (!long.TryParse(apiClientIdClaim, out var apiClientId))
            {
                _logger.LogWarning("Rejecting Zabbix alert {EventId}: API client ID claim is missing or invalid.", payload.EventId);
                return Unauthorized();
            }

            var apiClientEntity = await _clientRepository.GetByIdAsync(apiClientId);

            if (apiClientEntity == null)
            {
                _logger.LogWarning("Rejecting Zabbix alert {EventId}: API client ID {ClientId} not found in the database.", payload.EventId, apiClientId);
                return Unauthorized();
            }

            if (!apiClientEntity.IsActive)
            {
                _logger.LogWarning("Rejecting Zabbix alert {EventId}: API client ID {ClientId} is inactive.", payload.EventId, apiClientId);
                return Unauthorized();
            }

            var targetEntity = await _targetRepository.GetByDiscordIdAsync(apiClientId, targetDiscordId);

            if (targetEntity == null)
            {
                _logger.LogWarning("Rejecting Zabbix alert: API client ID {ClientId} is not authorized for target {TargetId}.", apiClientId, targetDiscordId);
                return Forbid();
            }

            try
            {
                _logger.LogDebug("Resolving Discord destination for target {TargetId}...", targetDiscordId);

                IMessageChannel? destination = null;
                var resolvedChannel = await _client.Rest.GetChannelAsync(targetDiscordId);
                IUser? resolvedUser = null;

                if (resolvedChannel is IMessageChannel msgChannel)
                {
                    destination = msgChannel;
                }
                else
                {
                    resolvedUser = await _client.Rest.GetUserAsync(targetDiscordId);
                    if (resolvedUser != null)
                    {
                        destination = await resolvedUser.CreateDMChannelAsync();
                    }
                }

                if (destination == null)
                {
                    _logger.LogWarning("Failed to deliver Zabbix alert {EventId}: Discord target {TargetId} could not be resolved to a valid channel or user.", payload.EventId, targetDiscordId);
                    return NotFound(new { error = "Target not found" });
                }

                targetEntity = await _targetSyncService.VerifyAndUpdateTargetAsync(targetEntity, resolvedChannel, resolvedUser);

                if (targetEntity == null)
                {
                    _logger.LogWarning("Failed to deliver Zabbix alert {EventId}: After synchronization, target {TargetId} could not be resolved. It may have been deleted or permissions changed.", payload.EventId, targetDiscordId);
                    return NotFound(new { error = "Target not found after synchronization" });
                }

                _logger.LogDebug("Building Discord UI components for event {EventId}...", payload.EventId);

                bool isDM = destination is IDMChannel;

                var componentsV2 = _discordUiService.CreateZabbixAlertContainer(payload, isDM, apiClientId);

                _logger.LogDebug("Dispatching message to Discord API for event {EventId}...", payload.EventId);
                var message = await destination.SendMessageAsync(components: componentsV2, flags: MessageFlags.ComponentsV2);

                // CROSSPOSTING LOGIC
                if (targetEntity.ChannelType == TextChannelType.GuildAnnouncementChannel && targetEntity.AutoCrosspost)
                {
                    if (message is IUserMessage userMessage)
                    {
                        try
                        {
                            await userMessage.CrosspostAsync();
                            _logger.LogInformation("Successfully crossposted event {EventId} in announcement channel {TargetId}", payload.EventId, targetDiscordId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to crosspost event {EventId} in channel {TargetId}. Check bot permissions (Manage Messages / Send Messages).", payload.EventId, targetDiscordId);
                        }
                    }
                }

                _logger.LogInformation("Successfully delivered Zabbix alert {EventId} to Discord target {TargetId}.", payload.EventId, targetDiscordId);
                return Ok(new { status = "Success" });
            }
            catch (Exception ex)
            {
                var errorId = CreateErrorId();
                _logger.LogError(ex, "An unexpected error occurred while processing Zabbix alert {EventId} for target {TargetId}. ErrorId: {ErrorId}", payload.EventId, targetDiscordId, errorId);

                return StatusCode(500, new
                {
                    error = "Internal server error during alert delivery.",
                    referenceId = errorId
                });
            }
        }

        private static string CreateErrorId()
        {
            return $"{ErrorIdPrefix}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
        }
    }
}
