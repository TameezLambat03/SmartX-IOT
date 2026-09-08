using System.Security.Cryptography;

namespace SmartXAPI.Services;

public class AttachmentEncryptionService
{
    private const int KeySize = 32;   // 256-bit key
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[] _key;

    public AttachmentEncryptionService(IConfiguration configuration)
    {
        string? configuredKey =
            configuration["AttachmentEncryptionKey"];

        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            throw new InvalidOperationException(
                "Attachment encryption key is not configured.");
        }

        try
        {
            _key = Convert.FromBase64String(configuredKey);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException(
                "Attachment encryption key is invalid.");
        }

        if (_key.Length != KeySize)
        {
            throw new InvalidOperationException(
                "Attachment encryption key must be exactly 256 bits.");
        }
    }

    public async Task EncryptFileAsync(
        Stream input,
        Stream output,
        CancellationToken cancellationToken = default)
    {
        using MemoryStream memoryStream = new();

        await input.CopyToAsync(
            memoryStream,
            cancellationToken);

        byte[] plaintext = memoryStream.ToArray();

        byte[] nonce =
            RandomNumberGenerator.GetBytes(NonceSize);

        byte[] ciphertext =
            new byte[plaintext.Length];

        byte[] tag =
            new byte[TagSize];

        using AesGcm aes =
            new AesGcm(_key, TagSize);

        aes.Encrypt(
            nonce,
            plaintext,
            ciphertext,
            tag);

        // File format:
        // [12-byte nonce]
        // [encrypted content]
        // [16-byte authentication tag]

        await output.WriteAsync(
            nonce,
            cancellationToken);

        await output.WriteAsync(
            ciphertext,
            cancellationToken);

        await output.WriteAsync(
            tag,
            cancellationToken);
    }
}