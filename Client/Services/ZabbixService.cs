using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Client.Models;
using Client.Data;
using Client.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Client.Data.Repositories;

namespace Client.Services
{
    public class ZabbixAcknowledgeResult
    {
        [JsonPropertyName("eventids")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long[] EventIds { get; set; } = [];
    }

    public class ZabbixService
    {
        private readonly HttpClient _httpClient;
        private readonly IDbContextFactory<ApiSecurityDbContext> _dbContextFactory;
        private readonly IEncryptionService _encryptionService;
        private readonly IApiSecurityStore _apiSecurityStore;
        private readonly IApiClientRepository _clientRepository;
        private readonly ILogger<ZabbixService> _logger;

        public ZabbixService(
            HttpClient httpClient,
            IDbContextFactory<ApiSecurityDbContext> dbContextFactory,
            IEncryptionService encryptionService,
            IApiSecurityStore apiSecurityStore,
            IApiClientRepository clientRepository,
            ILogger<ZabbixService> logger)
        {
            _httpClient = httpClient;
            _dbContextFactory = dbContextFactory;
            _encryptionService = encryptionService;
            _apiSecurityStore = apiSecurityStore;
            _clientRepository = clientRepository;
            _logger = logger;
        }

        private async Task<(string Url, string Token)> GetConnectionAsync(long apiClientId)
        {
            _logger.LogDebug("Retrieving Zabbix connection details from the database for API Client ID: {ApiClientId}", apiClientId);

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var connection = await dbContext.ZabbixCredentials
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.AssociatedIntegrationClientId == apiClientId);

            if (connection == null)
            {
                _logger.LogWarning("Failed to retrieve connection: No Zabbix configuration found for API Client ID: {ApiClientId}", apiClientId);
                throw new InvalidOperationException($"No Zabbix connection configured for client ID {apiClientId}");
            }

            _logger.LogDebug("Decrypting Zabbix API token for API Client ID: {ApiClientId}...", apiClientId);
            var decryptedToken = _encryptionService.Decrypt(connection.EncryptedApiToken);

            return (connection.ApiUrl, decryptedToken);
        }

        public async Task<bool> AcknowledgeEventAsync(long apiClientId, string eventId, string? message, bool shouldAck = true, bool shouldClose = false, int? newSeverity = null)
        {
            _logger.LogInformation("Attempting to update Zabbix event {EventId} (Client ID: {ApiClientId}). Ack: {ShouldAck}, Close: {ShouldClose}, NewSeverity: {NewSeverity}, HasMessage: {HasMessage}",
                eventId, apiClientId, shouldAck, shouldClose, newSeverity, !string.IsNullOrWhiteSpace(message));

            var isActive = await _clientRepository.IsActiveAsync(apiClientId);
            if (!isActive)
            {
                _logger.LogWarning("Rejecting event acknowledge request: API Client ID {ApiClientId} does not exist or is not active.", apiClientId);
                return false;
            }

            int action = 0;
            if (!string.IsNullOrWhiteSpace(message)) action |= 4;
            if (shouldAck) action |= 2; else action |= 16;
            if (shouldClose) action |= 1;
            if (newSeverity.HasValue) action |= 8;

            _logger.LogDebug("Calculated Zabbix API action bitmask: {ActionMask} for event {EventId}", action, eventId);

            var request = new ZabbixRequest
            {
                Method = "event.acknowledge",
                Params = new
                {
                    eventids = new[] { eventId },
                    action = action,
                    message = message,
                    severity = newSeverity
                }
            };

            var result = await SendRequestAsync<ZabbixAcknowledgeResult>(apiClientId, request);

            bool success = result != null && result.EventIds != null && result.EventIds.Length > 0;

            if (success)
            {
                _logger.LogInformation("Successfully updated Zabbix event {EventId}.", eventId);
            }
            else
            {
                _logger.LogWarning("Failed to update Zabbix event {EventId}. The API did not return the expected confirmation.", eventId);
            }

            return success;
        }

        public async Task<ZabbixEvent?> GetEventDetailsAsync(long apiClientId, string eventId)
        {
            _logger.LogDebug("Fetching details for Zabbix event {EventId} (API Client ID: {ApiClientId})...", eventId, apiClientId);

            var isActive = await _clientRepository.IsActiveAsync(apiClientId);
            if (!isActive)
            {
                _logger.LogWarning("Rejecting event details request: API Client ID {ApiClientId} does not exist or is not active.", apiClientId);
                return null;
            }

            var request = new ZabbixRequest
            {
                Method = "event.get",
                Params = new
                {
                    eventids = eventId,
                    output = new[] { "eventid", "severity", "acknowledged", "name" }
                }
            };

            var result = await SendRequestAsync<List<ZabbixEvent>>(apiClientId, request);
            var zabbixEvent = result?.FirstOrDefault();

            if (zabbixEvent != null)
            {
                _logger.LogDebug("Successfully retrieved details for Zabbix event {EventId}.", eventId);
            }
            else
            {
                _logger.LogWarning("Zabbix event {EventId} not found or could not be retrieved.", eventId);
            }

            return zabbixEvent;
        }

        public async Task<T?> SendRequestAsync<T>(long apiClientId, ZabbixRequest request)
        {
            try
            {
                _logger.LogDebug("Preparing Zabbix JSON-RPC request. Method: '{Method}'", request.Method);

                var (url, token) = await GetConnectionAsync(apiClientId);

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                string jsonPayload = JsonSerializer.Serialize(request, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                httpRequest.Content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

                _logger.LogDebug("Sending HTTP POST to Zabbix API at {Url}...", url);
                var response = await _httpClient.SendAsync(httpRequest);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Zabbix API responded with HTTP error {StatusCode} for method '{Method}'.", response.StatusCode, request.Method);
                    return default;
                }

                var content = await response.Content.ReadAsStringAsync();
                var zabbixResponse = JsonSerializer.Deserialize<ZabbixResponse<T>>(content);

                if (zabbixResponse == null)
                {
                    _logger.LogWarning("Failed to deserialize the JSON response from Zabbix for method '{Method}'.", request.Method);
                    return default;
                }

                if (zabbixResponse.Error != null)
                {
                    _logger.LogWarning(
                        "Zabbix API returned JSON-RPC error for method '{Method}'. Code: {Code}, Message: {Message}, Data: {Data}",
                        request.Method,
                        zabbixResponse.Error.Code,
                        zabbixResponse.Error.Message,
                        zabbixResponse.Error.Data);
                    return default;
                }

                if (zabbixResponse.Result == null)
                {
                    _logger.LogWarning("Zabbix API response for method '{Method}' did not include a result.", request.Method);
                    return default;
                }

                _logger.LogDebug("Zabbix API request '{Method}' completed successfully.", request.Method);
                return zabbixResponse.Result;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Cannot send Zabbix request. Initialization failed for API Client ID {ApiClientId}.", apiClientId);
                return default;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "A network error occurred while attempting to reach the Zabbix API. Method: '{Method}'", request.Method);
                return default;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while processing the Zabbix API request for method '{Method}'.", request.Method);
                return default;
            }
        }
    }
}
