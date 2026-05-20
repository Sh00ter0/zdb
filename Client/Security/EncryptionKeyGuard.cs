using System;
using Client.Models;
using Serilog;

namespace Client.Security
{
    public static class EncryptionKeyGuard
    {
        public static string EnsureKeyOrExit(AppApiConfig apiConfig)
        {
            var key = apiConfig.masterEncryptionKey;

            if (string.IsNullOrWhiteSpace(key))
            {
                return Exit("The 'api:masterEncryptionKey' value is missing. Expected environment variable: DZB_api__masterEncryptionKey. It must be a secure 32-byte Base64 string.");
            }

            byte[] decodedKey;
            try
            {
                decodedKey = Convert.FromBase64String(key);
            }
            catch (FormatException ex)
            {
                return Exit(ex, "The provided 'api:masterEncryptionKey' value is not valid Base64. Check environment variable: DZB_api__masterEncryptionKey.");
            }

            if (decodedKey.Length != 32)
            {
                return Exit($"The decoded 'api:masterEncryptionKey' has an invalid length of {decodedKey.Length} bytes (exactly 32 bytes required). Check environment variable: DZB_api__masterEncryptionKey.");
            }

            Log.Information("Master encryption key validated successfully.");

            return key;
        }

        private static string Exit(string msg)
        {
            return Exit(null, msg);
        }

        private static string Exit(Exception? ex, string msg)
        {
            Log.Fatal(ex, msg);
            Log.CloseAndFlush();
            Environment.Exit(1);
            return string.Empty;
        }
    }
}
