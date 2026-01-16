namespace Convoy.Service.Interfaces;

/// <summary>
/// AES-256 encryption/decryption service for securing API requests and responses
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypt string data using AES-256
    /// </summary>
    /// <param name="plainText">Plain text to encrypt</param>
    /// <returns>Base64 encoded encrypted string</returns>
    string Encrypt(string plainText);

    /// <summary>
    /// Decrypt AES-256 encrypted data
    /// </summary>
    /// <param name="cipherText">Base64 encoded encrypted string</param>
    /// <returns>Decrypted plain text</returns>
    string Decrypt(string cipherText);

    /// <summary>
    /// Check if encryption is enabled in configuration
    /// </summary>
    bool IsEnabled { get; }
}
