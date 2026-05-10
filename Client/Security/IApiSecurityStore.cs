using Client.Data;
using Client.Data.Repositories;
using Client.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Client.Security
{
    public interface IApiSecurityStore
    {
        Task InitializeAsync();
        Task<ApiClientValidationResult?> ValidateApiKeyAsync(string apiKey);
        Task<ApiClientCreationResult> CreateApiClientAsync(string name, string zabbixApiUrl, string zabbixApiToken);
        Task<string> RenewApiKeyAsync(long clientId);
        Task UpdateZabbixConnectionAsync(long clientId, string newUrl, string newToken);
    }

    public class ApiSecurityStore : IApiSecurityStore
    {
        private const string ApiKeyPrefix = "zdb";

        private readonly IApiClientRepository _apiClientRepository;
        private readonly IApiTargetRepository _apiTargetRepository;
        private readonly IBotAdminRepository _botAdminRepository;

        private readonly IDbContextFactory<ApiSecurityDbContext> _dbContextFactory;
        private readonly IEncryptionService _encryptionService;
        private readonly ILogger<ApiSecurityStore> _logger;
        private readonly byte[] _apiKeyHashPepperBytes;

        public ApiSecurityStore(
            IApiClientRepository apiClientRepository,
            IApiTargetRepository apiTargetRepository,
            IBotAdminRepository botAdminRepository,
            IDbContextFactory<ApiSecurityDbContext> dbContextFactory,
            IEncryptionService encryptionService,
            IOptions<AppApiConfig> apiConfig,
            ILogger<ApiSecurityStore> logger)
        {
            _apiClientRepository = apiClientRepository;
            _apiTargetRepository = apiTargetRepository;
            _botAdminRepository = botAdminRepository;
            _dbContextFactory = dbContextFactory;
            _encryptionService = encryptionService;
            _logger = logger;
            var apiKeyHashPepper = apiConfig.Value.apiKeyHashPepper;

            if (string.IsNullOrWhiteSpace(apiKeyHashPepper))
            {
                throw new ArgumentException("API key hash pepper must be configured.", nameof(apiConfig));
            }

            _apiKeyHashPepperBytes = Encoding.UTF8.GetBytes(apiKeyHashPepper);
        }

        public async Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Initializing API Security Store and applying database migrations...");
                await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
                await dbContext.Database.MigrateAsync();
                _logger.LogDebug("Database migrations applied successfully.");

                var testRecord = await dbContext.ZabbixCredentials.AsNoTracking().FirstOrDefaultAsync();

                if (testRecord != null)
                {
                    try
                    {
                        _encryptionService.Decrypt(testRecord.EncryptedApiToken);
                        _logger.LogDebug("Master encryption key validated against database records successfully.");
                    }
                    catch (CryptographicException)
                    {
                        _logger.LogCritical("CRITICAL: Master encryption key mismatch. The provided key cannot decrypt existing records. Restore the original key or recreate the database.");
                        Serilog.Log.CloseAndFlush();
                        Environment.Exit(1);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Critical database failure during initialization.");
                throw;
            }
        }

        public async Task<ApiClientValidationResult?> ValidateApiKeyAsync(string apiKey)
        {
            var keyPreview = CreateKeyPreview(apiKey);

            try
            {
                var apiKeyHash = ComputeHash(apiKey);
                var client = await _apiClientRepository.GetByKeyHashAsync(apiKeyHash);

                if (client != null && client.IsActive)
                {
                    return new ApiClientValidationResult(client.Id, client.Name);
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during API key validation. KeyPreview: {KeyPreview}", keyPreview);
                return null;
            }
        }

        public async Task<ApiClientCreationResult> CreateApiClientAsync(string name, string zabbixApiUrl, string zabbixApiToken)
        {
            var normalizedClientName = name.Trim();

            try
            {
                var apiKey = GenerateApiKey();
                var apiKeyHash = ComputeHash(apiKey);
                var keyPreview = CreateKeyPreview(apiKey);
                var encryptedZabbixToken = _encryptionService.Encrypt(zabbixApiToken);

                var entity = new IntegrationClientEntity
                {
                    Name = normalizedClientName,
                    KeyHash = apiKeyHash,
                    KeyPreview = keyPreview,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow,
                    ZabbixCredential = new ZabbixCredentialEntity
                    {
                        ApiUrl = zabbixApiUrl,
                        EncryptedApiToken = encryptedZabbixToken,
                        CreatedAtUtc = DateTime.UtcNow
                    }
                };

                var success = await _apiClientRepository.AddAsync(entity);
                if (!success) throw new InvalidOperationException("Failed to add the client to the database.");

                return new ApiClientCreationResult(entity.Id, normalizedClientName, apiKey);
            }
            catch
            {
                throw new InvalidOperationException($"Client `{normalizedClientName}` already exists or conflicts.");
            }
        }

        public async Task<string> RenewApiKeyAsync(long clientId)
        {
            var client = await _apiClientRepository.GetByIdAsync(clientId);
            if (client == null) throw new InvalidOperationException("API Client not found.");

            var newApiKey = GenerateApiKey();
            client.KeyHash = ComputeHash(newApiKey);
            client.KeyPreview = CreateKeyPreview(newApiKey);

            var success = await _apiClientRepository.UpdateAsync(client);
            if (!success) throw new InvalidOperationException("Failed to update the database with the new key.");

            return newApiKey;
        }

        public async Task UpdateZabbixConnectionAsync(long clientId, string newUrl, string newToken)
        {
            var client = await _apiClientRepository.GetByIdAsync(clientId);
            if (client == null) throw new InvalidOperationException("API Client not found.");

            if (client.ZabbixCredential == null)
            {
                client.ZabbixCredential = new ZabbixCredentialEntity { AssociatedIntegrationClientId = clientId, CreatedAtUtc = DateTime.UtcNow };
            }

            client.ZabbixCredential.ApiUrl = newUrl;
            client.ZabbixCredential.EncryptedApiToken = _encryptionService.Encrypt(newToken);
            client.ZabbixCredential.UpdatedAtUtc = DateTime.UtcNow;

            var success = await _apiClientRepository.UpdateAsync(client);
            if (!success) throw new InvalidOperationException("Failed to update Zabbix credentials in the database.");
        }

        private static string GenerateApiKey()
        {
            var keyBytes = RandomNumberGenerator.GetBytes(32);
            return $"{ApiKeyPrefix}_{Convert.ToHexString(keyBytes).ToLowerInvariant()}";
        }

        private static string CreateKeyPreview(string apiKey)
        {
            return apiKey.Length <= 10 ? apiKey : $"{apiKey[..7]}...{apiKey[^4..]}";
        }

        private string ComputeHash(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            using var hmac = new HMACSHA256(_apiKeyHashPepperBytes);
            var hash = hmac.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }
    }
}
