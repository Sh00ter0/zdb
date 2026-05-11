using Client.Services;
using Serilog;

namespace Client.Middleware
{
    public class DiscordStatusMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly DiscordStateService _stateService;
        private readonly ILogger<DiscordStatusMiddleware> _logger;
        public DiscordStatusMiddleware(RequestDelegate next, DiscordStateService stateService, ILogger<DiscordStatusMiddleware> logger)
        {
            _next = next;
            _stateService = stateService;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!_stateService.IsReady)
            {
                _logger.LogWarning("Discord client is not connected or ready.");
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Discord client not ready",
                    message = "The Discord client is still connecting or initializing. Please try again shortly."
                });

                return;
            }
            await _next(context);
        }
    }
}
