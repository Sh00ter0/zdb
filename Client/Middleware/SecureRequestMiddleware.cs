using Application.Exceptions.API;
using Client.Models;
using Microsoft.Extensions.Options;

namespace Client.Middleware
{
    public class SecureRequestMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment _env;
        private readonly IOptions<AppApiConfig> _config;
        public SecureRequestMiddleware(RequestDelegate next,
            IWebHostEnvironment env,
            IOptions<AppApiConfig> config)
        {
            _next = next;
            _env = env;
            _config = config;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!_config.Value.allowInsecureHttp && !_env.IsDevelopment() && !context.Request.IsHttps)
            {
                throw new ProblemException("HTTPS required", "This API only accepts secure HTTPS requests", StatusCodes.Status400BadRequest);
            }
            await _next(context);
        }
    }
}
