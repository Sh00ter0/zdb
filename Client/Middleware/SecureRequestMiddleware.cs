using Client.Models;
using Microsoft.Extensions.Options;
using Serilog;

namespace Client.Middleware
{
    public class SecureRequestMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment _env;
        private readonly IOptions<AppApiConfig> _config;
        private readonly ILogger<SecureRequestMiddleware> _logger;
        public SecureRequestMiddleware(RequestDelegate next,
            IWebHostEnvironment env,
            IOptions<AppApiConfig> config,
            ILogger<SecureRequestMiddleware> logger)
        {
            _next = next;
            _env = env;
            _config = config;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!_config.Value.allowInsecureHttp && !_env.IsDevelopment() && !context.Request.IsHttps)
            {
                _logger.LogWarning("Rejected insecure HTTP request for path {Path}", context.Request.Path);

                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "HTTPS required",
                    message = "This API only accepts secure HTTPS requests."
                });

                return;
            }
            await _next(context);
        }
    }
}
