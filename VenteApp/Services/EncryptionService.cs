using System.Security.Cryptography;
using System.Text;

public class EncryptionService
{
    private static EncryptionService _instance;
    public static EncryptionService Instance => _instance ??= new EncryptionService();

    private readonly string _encryptionKey = "encryption-key-93697BD6-3E80-413F-B19D-3E344DD58DF1"; // Ensure this is a secure key

    // Private constructor to prevent direct instantiation
    private EncryptionService() { }

    public string Encrypt(string plainText)
    {
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        using (Aes aes = Aes.Create())
        {
            aes.Key = GetValidKey(_encryptionKey); // Get a valid key of the required length
            aes.IV = new byte[16]; // Initialization vector (16 bytes)

            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cryptoStream.Write(plainBytes, 0, plainBytes.Length);
                    cryptoStream.FlushFinalBlock();
                }
                return Convert.ToBase64String(memoryStream.ToArray());
            }
        }
    }

    public string Decrypt(string encryptedText)
    {
        byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
        using (Aes aes = Aes.Create())
        {
            aes.Key = GetValidKey(_encryptionKey); // Get a valid key of the required length
            aes.IV = new byte[16]; // Initialization vector (16 bytes)

            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cryptoStream.Write(encryptedBytes, 0, encryptedBytes.Length);
                    cryptoStream.FlushFinalBlock();
                }
                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }
    }

    // Helper method to ensure the encryption key is the correct length
    private byte[] GetValidKey(string key)
    {
        // Convert the key to bytes
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);

        // Ensure the key is exactly 32 bytes long (256-bit key)
        if (keyBytes.Length == 32)
            return keyBytes;
        else if (keyBytes.Length > 32)
            Array.Resize(ref keyBytes, 32); // Truncate if too long
        else
            Array.Resize(ref keyBytes, 32); // Pad with zeros if too short

        return keyBytes;
    }
}
