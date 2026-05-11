using System.Security.Cryptography;
using System.Text;

namespace Client.Security
{
    public interface IEncryptionService
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
    }

    public class EncryptionService : IEncryptionService
    {
        private const string AeadPrefix = "v2:";
        private const int NonceSize = 12;
        private const int TagSize = 16;

        private readonly byte[] _masterKey;
        private readonly ILogger<EncryptionService> _logger;

        public EncryptionService(string base64MasterKey, ILogger<EncryptionService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            try
            {
                _masterKey = Convert.FromBase64String(base64MasterKey);

                if (_masterKey.Length != 32)
                {
                    throw new ArgumentException("Master key must be exactly 32 bytes.");
                }

                _logger.LogInformation("Encryption service initialized successfully with a secure 256-bit key.");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to initialize the Encryption Service. The provided master key is invalid.");
                throw;
            }
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return plainText;
            }

            try
            {
                _logger.LogDebug("Starting authenticated data encryption (AES-GCM)...");

                byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] cipherBytes = new byte[plainBytes.Length];
                byte[] tag = new byte[TagSize];

                using var aesGcm = new AesGcm(_masterKey, TagSize);
                aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);

                byte[] resultBytes = new byte[nonce.Length + tag.Length + cipherBytes.Length];
                Buffer.BlockCopy(nonce, 0, resultBytes, 0, nonce.Length);
                Buffer.BlockCopy(tag, 0, resultBytes, nonce.Length, tag.Length);
                Buffer.BlockCopy(cipherBytes, 0, resultBytes, nonce.Length + tag.Length, cipherBytes.Length);

                _logger.LogDebug("Data encrypted successfully.");
                return AeadPrefix + Convert.ToBase64String(resultBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during data encryption.");
                throw new CryptographicException("Encryption failed.", ex);
            }
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
            {
                return cipherText;
            }

            try
            {
                _logger.LogDebug("Starting data decryption...");

                if (cipherText.StartsWith(AeadPrefix, StringComparison.Ordinal))
                {
                    return DecryptAeadPayload(cipherText[AeadPrefix.Length..]);
                }

                throw new CryptographicException("Unsupported cipher format. Expected AES-GCM payload with 'v2:' prefix.");
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Decryption failed. The provided cipher text is not a valid Base64 string.");
                throw new CryptographicException("Decryption failed due to invalid format.", ex);
            }
            catch (CryptographicException ex)
            {
                _logger.LogError(ex, "Decryption failed. This is typically caused by corrupted data, a mismatched key, or bad padding.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during data decryption.");
                throw new CryptographicException("Decryption failed due to an unexpected error.", ex);
            }
        }

        private string DecryptAeadPayload(string payloadBase64)
        {
            byte[] fullCipherBytes = Convert.FromBase64String(payloadBase64);

            if (fullCipherBytes.Length < NonceSize + TagSize)
            {
                throw new CryptographicException("Cipher text is too short for AES-GCM payload.");
            }

            byte[] nonce = new byte[NonceSize];
            byte[] tag = new byte[TagSize];
            int cipherLength = fullCipherBytes.Length - NonceSize - TagSize;
            byte[] cipherBytes = new byte[cipherLength];
            byte[] plainBytes = new byte[cipherLength];

            Buffer.BlockCopy(fullCipherBytes, 0, nonce, 0, nonce.Length);
            Buffer.BlockCopy(fullCipherBytes, nonce.Length, tag, 0, tag.Length);
            Buffer.BlockCopy(fullCipherBytes, nonce.Length + tag.Length, cipherBytes, 0, cipherBytes.Length);

            using var aesGcm = new AesGcm(_masterKey, TagSize);
            aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);

            _logger.LogDebug("Data decrypted successfully using AES-GCM.");
            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}
