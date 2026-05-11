using Application.Repositories;
using Client.Policies.Requirements;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Client.Policies.Handlers
{
    public class TargetAccessHandler : AuthorizationHandler<TargetAccessRequirement>
    {
        private readonly ILogger<TargetAccessHandler> _logger;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IKnownDeliveryTargetRepository _targetRepository;
        public TargetAccessHandler(ILogger<TargetAccessHandler> logger,
            IHttpContextAccessor contextAccessor,
            IKnownDeliveryTargetRepository targetRepository)
        {
            _logger = logger;
            _contextAccessor = contextAccessor;
            _targetRepository = targetRepository;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, TargetAccessRequirement requirement)
        {
            try
            {
                var clientId = GetClientIdFromClaim(context);
                var targetId = GetTargetId();
                await VerifyAccess(clientId, targetId);

                context.Succeed(requirement);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message);
                context.Fail();
                return;
            }
        }

        private async Task VerifyAccess(long clientId, ulong targetId)
        {
            var target = await _targetRepository.GetByDiscordIdAsync(clientId, targetId);
            if (target is null) throw new Exception($"Client id: {clientId}, is not authorized to access target: {targetId}");
        }

        private ulong GetTargetId()
        {
            var httpContext = _contextAccessor.HttpContext;
            if (httpContext is null) throw new Exception("Missing http context");

            var request = httpContext.Request;
            var target = request.RouteValues["targetDiscordId"]?.ToString();
            var isValid = ulong.TryParse(target, out ulong targetId);

            if (!isValid) throw new Exception("Target Id is not valid.");

            return targetId;
        }

        private long GetClientIdFromClaim(AuthorizationHandlerContext context)
        {
            var apiClientId = context.User.FindFirstValue("ApiClientId");
            var isValid = long.TryParse(apiClientId, out long clientId);

            if (!isValid) throw new Exception("Client Id is not valid.");

            return clientId;
        }
    }
}
