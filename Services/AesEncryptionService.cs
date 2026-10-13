using System.Security.Cryptography;
using System.Text;

namespace AppealsSystem.Services
{
    public class AesEncryptionService : IEncryptionService
    {
        private readonly byte[] _key;
        private readonly byte[] _iv = new byte[16];

        public AesEncryptionService(IConfiguration config)
        {
            var keyString = config["Encryption:Key"] ?? "0123456789abcdef0123456789abcdef";
            _key = Encoding.UTF8.GetBytes(keyString);
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;
            using var aes = Aes.Create();
            aes.Key = _key; aes.IV = _iv;
            using var enc = aes.CreateEncryptor();
            var bytes = Encoding.UTF8.GetBytes(plainText);
            var result = enc.TransformFinalBlock(bytes, 0, bytes.Length);
            return Convert.ToBase64String(result);
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;
            using var aes = Aes.Create();
            aes.Key = _key; aes.IV = _iv;
            using var dec = aes.CreateDecryptor();
            var bytes = Convert.FromBase64String(cipherText);
            var result = dec.TransformFinalBlock(bytes, 0, bytes.Length);
            return Encoding.UTF8.GetString(result);
        }
    }
}