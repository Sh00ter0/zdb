using Application.Exceptions.API;
using Infrastructure.Services.Discord;

namespace Client.Middleware
{
    public class DiscordStatusMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly DiscordStateService _stateService;
        public DiscordStatusMiddleware(RequestDelegate next, DiscordStateService stateService)
        {
            _next = next;
            _stateService = stateService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!_stateService.IsOperational)
            {
                throw new ProblemException(
                    "Discord Temporarily Unavailable",
                    $"The Discord gateway is currently offline. State: { _stateService.HealthState}",
                    StatusCodes.Status503ServiceUnavailable);
            }
            await _next(context);
        }
    }
}
