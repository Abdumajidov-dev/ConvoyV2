using Convoy.Service.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Convoy.Service.Services;

/// <summary>
/// AES-256-CBC encryption service for securing API communication
/// Key va IV configuration dan olinadi
/// </summary>
public class EncryptionService : IEncryptionService
{
    private readonly ILogger<EncryptionService> _logger;
    private readonly byte[] _key;
    private readonly bool _isEnabled;

    public bool IsEnabled => _isEnabled;

    public EncryptionService(IConfiguration configuration, ILogger<EncryptionService> logger)
    {
        _logger = logger;

        // Encryption enabled/disabled check
        var enabledValue = configuration["Encryption:Enabled"];
        _isEnabled = !string.IsNullOrEmpty(enabledValue) && bool.Parse(enabledValue);

        if (!_isEnabled)
        {
            _logger.LogWarning("⚠️ Encryption is DISABLED. Set 'Encryption:Enabled' to true in production!");
            _key = Array.Empty<byte>();
            return;
        }

        // AES-256 uchun 32 byte key kerak (IV har safar random generate bo'ladi)
        var keyString = configuration["Encryption:Key"];

        if (string.IsNullOrEmpty(keyString))
        {
            throw new InvalidOperationException(
                "Encryption is enabled but Key is missing in configuration. " +
                "Generate key using: openssl rand -base64 32 (for Key)");
        }

        try
        {
            _key = Convert.FromBase64String(keyString);

            // Key validation
            if (_key.Length != 32)
            {
                throw new InvalidOperationException($"AES-256 requires 32 byte key. Current key is {_key.Length} bytes.");
            }

            _logger.LogInformation("✅ Encryption service initialized successfully (AES-256-CBC with random IV per request)");
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("Invalid Key format. Must be Base64 encoded.", ex);
        }
    }

    public string Encrypt(string plainText)
    {
        if (!_isEnabled)
        {
            _logger.LogDebug("Encryption disabled, returning plain text");
            return plainText;
        }

        if (string.IsNullOrEmpty(plainText))
        {
            throw new ArgumentNullException(nameof(plainText));
        }

        try
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            // Har bir request uchun random IV generate qilish
            aes.GenerateIV();
            var iv = aes.IV;

            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using var msEncrypt = new MemoryStream();
            using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(plainText);
            }

            var encrypted = msEncrypt.ToArray();

            // IV ni encrypted data bilan birga jo'natish (IV + encrypted data)
            var combinedData = new byte[iv.Length + encrypted.Length];
            Buffer.BlockCopy(iv, 0, combinedData, 0, iv.Length);
            Buffer.BlockCopy(encrypted, 0, combinedData, iv.Length, encrypted.Length);

            var result = Convert.ToBase64String(combinedData);

            _logger.LogDebug("Encrypted data length: {Length} bytes (IV: {IvLength}, Data: {DataLength})",
                combinedData.Length, iv.Length, encrypted.Length);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Encryption failed");
            throw new InvalidOperationException("Encryption failed", ex);
        }
    }

    public string Decrypt(string cipherText)
    {
        if (!_isEnabled)
        {
            _logger.LogDebug("Encryption disabled, returning cipher text as-is");
            return cipherText;
        }

        if (string.IsNullOrEmpty(cipherText))
        {
            throw new ArgumentNullException(nameof(cipherText));
        }

        try
        {
            // Clean the input: remove whitespace, newlines, quotes
            var cleanedCipherText = cipherText
                .Trim()
                .Replace("\n", "")
                .Replace("\r", "")
                .Replace(" ", "")
                .Replace("\t", "")
                .Trim('"')
                .Trim('\'');

            _logger.LogDebug("Decrypting data. Original length: {Original}, Cleaned length: {Cleaned}",
                cipherText.Length, cleanedCipherText.Length);

            // Support both standard and URL-safe Base64
            // URL-safe: - becomes +, _ becomes /
            var base64String = cleanedCipherText.Replace('-', '+').Replace('_', '/');

            // Add padding if needed (URL-safe Base64 might not have padding)
            var padding = (4 - (base64String.Length % 4)) % 4;
            if (padding > 0)
            {
                base64String += new string('=', padding);
            }

            var combinedData = Convert.FromBase64String(base64String);

            // IV ni ajratib olish (birinchi 16 byte)
            if (combinedData.Length < 16)
            {
                throw new InvalidOperationException("Invalid encrypted data: too short to contain IV");
            }

            var iv = new byte[16];
            var encryptedData = new byte[combinedData.Length - 16];

            Buffer.BlockCopy(combinedData, 0, iv, 0, 16);
            Buffer.BlockCopy(combinedData, 16, encryptedData, 0, encryptedData.Length);

            _logger.LogDebug("Extracted IV ({IvLength} bytes) and encrypted data ({DataLength} bytes)",
                iv.Length, encryptedData.Length);

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = iv;  // Request bilan kelgan IV ni ishlatish
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using var msDecrypt = new MemoryStream(encryptedData);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);

            var result = srDecrypt.ReadToEnd();
            _logger.LogDebug("Decrypted data successfully");
            return result;
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Invalid Base64 format");
            throw new InvalidOperationException("Invalid encrypted data format", ex);
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Decryption failed - possibly wrong key/IV");
            throw new InvalidOperationException("Decryption failed", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Decryption failed");
            throw new InvalidOperationException("Decryption failed", ex);
        }
    }
}
