using Application.Services.API;
using Client.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Client.Handlers
{
    public class ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IProblemDetailsService details,
        IOptions<AppApiConfig> apiConfig,
        IApiSecurityStore apiSecurityStore)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        private readonly AppApiConfig _apiConfig = apiConfig.Value;

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(_apiConfig.headerName, out var headerValues))
            {
                return AuthenticateResult.NoResult();
            }

            if (headerValues.Count != 1 || string.IsNullOrWhiteSpace(headerValues[0]))
            {
                return AuthenticateResult.Fail("Invalid API key header.");
            }

            var providedApiKey = headerValues[0]!.Trim();

            var matchedKey = await apiSecurityStore.ValidateApiKeyAsync(providedApiKey);
            if (matchedKey == null)
            {
                return AuthenticateResult.Fail("Invalid API key.");
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, matchedKey.Name),
                new Claim(CustomClaimTypes.ApiKeyName, matchedKey.Name),
                new Claim(CustomClaimTypes.ApiClientId, matchedKey.ClientId.ToString())
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }

        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            await WriteResponse(StatusCodes.Status401Unauthorized, "Unauthorized", "Invalid API key.");
        }

        protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
        {
            await WriteResponse(StatusCodes.Status403Forbidden, "Forbidden",
                "The authenticated client is not allowed to access this resource.");
        }

        private async Task WriteResponse(int statusCode, string error, string message)
        {
            Response.StatusCode = statusCode;
            await details.TryWriteAsync(new ProblemDetailsContext()
            {
                HttpContext = Context,
                ProblemDetails = new ProblemDetails()
                {
                    Status = statusCode,
                    Type = error,
                    Detail = message
                }
            });
        }
    }
}