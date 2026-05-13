using Application.Common.Zabbix;
using Domain.Enums;
using Infrastructure.Services.API;
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
        private readonly ILogger<ZabbixController> _logger;

        private readonly DiscordAlertService _alertService;

        public ZabbixController(
            ILogger<ZabbixController> logger,
            DiscordAlertService alertService)
        {
            _alertService = alertService;
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

            await _alertService.ProcessAlertAsync(apiClientId, targetDiscordId, payload);
            return Ok(new { status = "Success" });
        }
    }
}
