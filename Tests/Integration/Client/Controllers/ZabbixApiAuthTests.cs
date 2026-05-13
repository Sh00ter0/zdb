using Application.Common.API;
using Application.Repositories;
using Application.Services.API;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net;
using System.Net.Http.Json;

namespace Tests.Integration.Client.Controllers;

/// <summary>
/// Integration tests for the Zabbix API authentication and authorization flow.
/// Utilizes WebApplicationFactory to run the API in-memory.
/// </summary>
public class ZabbixApiAuthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IApiSecurityStore> _mockSecurityStore;
    private readonly Mock<IKnownDeliveryTargetRepository> _mockTargetRepository;

    public ZabbixApiAuthTests(WebApplicationFactory<Program> factory)
    {
        _mockSecurityStore = new Mock<IApiSecurityStore>();
        _mockTargetRepository = new Mock<IKnownDeliveryTargetRepository>();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Remove the actual database repositories and replace them with mocks
                // to isolate the authentication logic from the database layer.
                var storeDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IApiSecurityStore));
                if (storeDescriptor != null) services.Remove(storeDescriptor);

                var targetDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IKnownDeliveryTargetRepository));
                if (targetDescriptor != null) services.Remove(targetDescriptor);

                // Register the mocked instances
                services.AddSingleton(_mockSecurityStore.Object);
                services.AddSingleton(_mockTargetRepository.Object);
            });
        });
    }

    [Fact]
    public async Task ReceiveAlert_MissingApiKeyHeader_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var payload = new { EventId = "12345", Description = "Test Alert" };

        // Act - Send request WITHOUT the authorization header
        var response = await client.PostAsJsonAsync("/api/zabbix/123456789", payload);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReceiveAlert_InvalidApiKey_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var payload = new { EventId = "12345" };
        var invalidApiKey = "invalid-token-123";

        client.DefaultRequestHeaders.Add("Api-Key", invalidApiKey);

        // Setup mock to return null for an invalid key
        _mockSecurityStore
            .Setup(x => x.ValidateApiKeyAsync(invalidApiKey))
            .ReturnsAsync((ApiClientValidationResult?)null);

        // Act
        var response = await client.PostAsJsonAsync("/api/zabbix/123456789", payload);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReceiveAlert_ValidKeyButNotAuthorizedForTarget_Returns403Forbidden()
    {
        // Arrange
        var client = _factory.CreateClient();
        var payload = new { EventId = "12345" };
        var validApiKey = "valid-token-123";
        ulong targetDiscordId = 123456789;
        long apiClientId = 99;

        client.DefaultRequestHeaders.Add("Api-Key", validApiKey);

        var fakeClientEntity = new ApiClientValidationResult(apiClientId, "TestClient");

        // Mock successful API key validation
        _mockSecurityStore
            .Setup(x => x.ValidateApiKeyAsync(validApiKey))
            .ReturnsAsync(fakeClientEntity);

        // Mock authorization failure: the client does not have access to this specific target
        _mockTargetRepository
            .Setup(x => x.GetByDiscordIdAsync(apiClientId, targetDiscordId))
            .ReturnsAsync((KnownDeliveryTargets?)null);

        // Act
        var response = await client.PostAsJsonAsync($"/api/zabbix/{targetDiscordId}", payload);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ReceiveAlert_ValidKeyAndAuthorizedForTarget_PassesAuthAndReachesController()
    {
        // Arrange
        var client = _factory.CreateClient();
        var payload = new { EventId = "2137" };
        var validApiKey = "valid-token-123";
        ulong targetDiscordId = 123456789;
        long apiClientId = 99;

        client.DefaultRequestHeaders.Add("Api-Key", validApiKey);

        var fakeClientEntity = new ApiClientValidationResult(apiClientId, "TestClient");

        var fakeTargetEntity = new KnownDeliveryTargets
        {
            TargetId = targetDiscordId,
            IntegrationClientId = apiClientId,
            Name = "Test Target",
            CreatedAtUtc = DateTime.UtcNow
        };

        // Mock successful API key validation and target authorization
        _mockSecurityStore
            .Setup(x => x.ValidateApiKeyAsync(validApiKey))
            .ReturnsAsync(fakeClientEntity);

        _mockTargetRepository
            .Setup(x => x.GetByDiscordIdAsync(apiClientId, targetDiscordId))
            .ReturnsAsync(fakeTargetEntity);

        // Act
        var response = await client.PostAsJsonAsync($"/api/zabbix/{targetDiscordId}", payload);

        // Assert
        // We ensure that the request passed the authentication and authorization middleware.
        // A status code other than 401 or 403 (like 400 BadRequest or 500) indicates that 
        // the request successfully reached the controller logic.
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }
}