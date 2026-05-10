using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Client.Models;
using Client.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Client.Handlers
{
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly AppApiConfig _apiConfig;
        private readonly IApiSecurityStore _apiSecurityStore;

        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IOptions<AppApiConfig> apiConfig,
            IApiSecurityStore apiSecurityStore)
            : base(options, logger, encoder)
        {
            _apiConfig = apiConfig.Value;
            _apiSecurityStore = apiSecurityStore;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            Logger.LogDebug("Evaluating API key authentication for request to {Path}...", Request.Path);

            if (!Request.Headers.TryGetValue(_apiConfig.headerName, out var headerValues))
            {
                Logger.LogDebug("Authentication skipped: No '{HeaderName}' header found in the request.", _apiConfig.headerName);
                return AuthenticateResult.NoResult();
            }

            if (headerValues.Count != 1 || string.IsNullOrWhiteSpace(headerValues[0]))
            {
                var remoteIp = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";
                Logger.LogWarning("Authentication failed: Invalid or multiple '{HeaderName}' headers received from {RemoteIp}.", _apiConfig.headerName, remoteIp);
                return AuthenticateResult.Fail("Invalid API key header.");
            }

            var providedApiKey = headerValues[0]!.Trim();

            var matchedKey = await _apiSecurityStore.ValidateApiKeyAsync(providedApiKey);

            if (matchedKey == null)
            {
                var remoteIp = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";
                Logger.LogWarning("Authentication failed: Invalid API key provided from {RemoteIp}.", remoteIp);
                return AuthenticateResult.Fail("Invalid API key.");
            }

            Logger.LogInformation("Successfully authenticated API request for client: {ClientName} (ID: {ClientId})", matchedKey.Name, matchedKey.ClientId);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, matchedKey.Name),
                new Claim("ApiKeyName", matchedKey.Name),
                new Claim("ApiClientId", matchedKey.ClientId.ToString())
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }

        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            var remoteIp = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";
            Logger.LogDebug("Authentication challenge triggered. Returning 401 Unauthorized to {RemoteIp}.", remoteIp);

            Response.StatusCode = StatusCodes.Status401Unauthorized;
            Response.ContentType = "application/json";

            await Response.WriteAsJsonAsync(new
            {
                error = "Unauthorized",
                message = $"Provide a valid API key in the '{_apiConfig.headerName}' header."
            });
        }

        protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
        {
            var remoteIp = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";
            Logger.LogWarning("Authorization forbidden. Authenticated client lacks permissions to access {Path}. Returning 403 Forbidden to {RemoteIp}.", Request.Path, remoteIp);

            Response.StatusCode = StatusCodes.Status403Forbidden;
            Response.ContentType = "application/json";

            await Response.WriteAsJsonAsync(new
            {
                error = "Forbidden",
                message = "The authenticated client is not allowed to access this resource."
            });
        }
    }
}
