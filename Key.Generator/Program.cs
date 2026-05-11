using System.Security.Cryptography;

namespace Key.Generator;

internal static class Program
{
    private const int SecretBytes = 32;

    private static void Main()
    {
        var apiKeyHashPepper = GenerateBase64Secret();
        var masterEncryptionKey = GenerateBase64Secret();

        ValidateMasterEncryptionKey(masterEncryptionKey);

        Console.WriteLine("Discord Zabbix Bridge secret generator");
        Console.WriteLine();
        Console.WriteLine("Generated production-ready secrets:");
        Console.WriteLine();
        Console.WriteLine($"DZB_api__apiKeyHashPepper={apiKeyHashPepper}");
        Console.WriteLine($"DZB_api__masterEncryptionKey={masterEncryptionKey}");
        Console.WriteLine();
        Console.WriteLine("PowerShell:");
        Console.WriteLine($"$env:DZB_api__apiKeyHashPepper=\"{apiKeyHashPepper}\"");
        Console.WriteLine($"$env:DZB_api__masterEncryptionKey=\"{masterEncryptionKey}\"");
        Console.WriteLine();
        Console.WriteLine("Notes:");
        Console.WriteLine("- masterEncryptionKey is Base64 for exactly 32 random bytes, required by AES-256-GCM.");
        Console.WriteLine("- apiKeyHashPepper is a high-entropy secret string used as the HMAC-SHA256 key.");
        Console.WriteLine("- Store both values in environment variables or a secret manager, not in appsettings.json.");
        Console.WriteLine("- Keep the masterEncryptionKey stable after data is encrypted. Rotating it without migration prevents decrypting existing Zabbix tokens.");
    }

    private static string GenerateBase64Secret()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(SecretBytes));
    }

    private static void ValidateMasterEncryptionKey(string masterEncryptionKey)
    {
        var decoded = Convert.FromBase64String(masterEncryptionKey);
        if (decoded.Length != SecretBytes)
        {
            throw new InvalidOperationException("Generated masterEncryptionKey is invalid.");
        }
    }
}
