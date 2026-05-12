using Application.Common.API;

namespace Application.Services.API
{
    public interface IApiSecurityStore
    {
        Task InitializeAsync();
        Task<ApiClientValidationResult?> ValidateApiKeyAsync(string apiKey);
        Task<ApiClientCreationResult> CreateApiClientAsync(string name, string zabbixApiUrl, string zabbixApiToken);
        Task<string> RenewApiKeyAsync(long clientId);
        Task UpdateZabbixConnectionAsync(long clientId, string newUrl, string newToken);
    }
}
