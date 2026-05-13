using System;
using System.Runtime.CompilerServices;

namespace Tests.Common;

/// <summary>
/// Global initializer for test environment variables.
/// Executes exactly once before any test runs in this assembly, ensuring thread-safe setup.
/// </summary>
public static class TestEnvironmentInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        // Inject fake environment variables to bypass the HashiCorp Vault validation 
        // and satisfy the minimal API startup requirements.
        // Note: Fake tokens, do not try to use them in production :-)
        Environment.SetEnvironmentVariable("DZB_SECRET_PROVIDER", "Local");
        Environment.SetEnvironmentVariable("DZB_discord__apiToken", "MTEyMjMzNDQ1NTY2Nzc4ODk5.Gxyz12.1234567890abcdefghijklmnopqrstuvwxyz123");
        Environment.SetEnvironmentVariable("DZB_api__apiKeyHashPepper", "test-dummy-pepper");
        Environment.SetEnvironmentVariable("DZB_api__masterEncryptionKey", "MDEyMzQ1Njc4OTAxMjM0NTY3ODkwMTIzNDU2Nzg5MDE=");

        // Override appsettings.json values specifically for the testing environment
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("DZB_api__headerName", "Api-Key");
        Environment.SetEnvironmentVariable("DZB_api__databasePath", "test_security.db");
        Environment.SetEnvironmentVariable("DZB_api__rateLimitPermit", "10");
        Environment.SetEnvironmentVariable("DZB_api__rateLimitWindowSeconds", "60");
    }
}