using Application.Common.Zabbix;
using Application.Repositories;
using Application.Services.Discord;
using Discord;
using Discord.WebSocket;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace Client.Controllers
{
    [ApiController]
    [Authorize(Policy = Policy.ZabbixIngress)]
    [EnableRateLimiting("zabbix-api")]
    [Route("api/[controller]")]
    public class ZabbixController : ControllerBase
    {
        private const string ErrorIdPrefix = "DZB";

        private readonly DiscordSocketClient _client;
        private readonly IKnownDeliveryTargetRepository _targetRepository;
        private readonly IDiscordTargetSyncService _targetSyncService;
        private readonly IDiscordUiService _discordUiService;
        private readonly ILogger<ZabbixController> _logger;

        public ZabbixController(
            DiscordSocketClient client,
            IKnownDeliveryTargetRepository targetRepository,
            IDiscordTargetSyncService targetSyncService,
            IDiscordUiService discordUiService,
            ILogger<ZabbixController> logger)
        {
            _client = client;
            _targetRepository = targetRepository;
            _targetSyncService = targetSyncService;
            _discordUiService = discordUiService;
            _logger = logger;
        }

        [HttpPost("{targetDiscordId}")]
        [Authorize(Policy = Policy.TargetAccess)]
        [RequestSizeLimit(1024 * 1024)]
        public async Task<IActionResult> ReceiveAlert(ulong targetDiscordId, [FromBody] ZabbixPayload payload)
        {
            _logger.LogInformation("Received Zabbix alert payload for Discord target {TargetId}. Event ID: {EventId}", targetDiscordId, payload.EventId);
            var apiClientIdClaim = User.FindFirstValue("ApiClientId");
            long.TryParse(apiClientIdClaim, out var apiClientId);

            var targetEntity = await _targetRepository.GetByDiscordIdAsync(apiClientId, targetDiscordId);

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
