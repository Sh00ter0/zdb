using System;
using Client.Models;
using Serilog;

namespace Client.Security
{
    public static class EncryptionKeyGuard
    {
        public static string EnsureKeyOrExit(AppApiConfig apiConfig)
        {
            Log.Information("Starting master encryption key validation...");

            var key = apiConfig.masterEncryptionKey;

            if (string.IsNullOrWhiteSpace(key))
            {
                Log.Fatal("CRITICAL: The 'api:masterEncryptionKey' value is missing. Expected environment variable: DZB_api__masterEncryptionKey. It must be a secure 32-byte Base64 string.");
                Log.CloseAndFlush();
                Environment.Exit(1);
                return string.Empty;
            }

            byte[] decodedKey;
            try
            {
                decodedKey = Convert.FromBase64String(key);
            }
            catch (FormatException ex)
            {
                Log.Fatal(ex, "CRITICAL: The provided 'api:masterEncryptionKey' value is not valid Base64. Check environment variable: DZB_api__masterEncryptionKey.");
                Log.CloseAndFlush();
                Environment.Exit(1);
                return string.Empty;
            }

            if (decodedKey.Length != 32)
            {
                Log.Fatal("CRITICAL: The decoded 'api:masterEncryptionKey' has an invalid length of {KeyLength} bytes (exactly 32 bytes required). Check environment variable: DZB_api__masterEncryptionKey.", decodedKey.Length);
                Log.CloseAndFlush();
                Environment.Exit(1);
                return string.Empty;
            }

            Log.Information("Master encryption key validated successfully.");

            return key;
        }
    }
}
