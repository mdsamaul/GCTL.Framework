using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace GCTL.UI.Core.Helpers
{
    public static class ConnectionEncryptor
    {
        // 32-character / 256-bit encryption key
        private const string SecretKey = "GCTL#Enterprise#SecretKey#2026!!";
        private static readonly byte[] KeyBytes = Encoding.UTF8.GetBytes(SecretKey);
        private static readonly byte[] FixedIv = new byte[16] { 15, 28, 73, 44, 95, 61, 82, 13, 94, 55, 66, 77, 88, 19, 30, 41 };

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            using var aes = Aes.Create();
            aes.Key = KeyBytes;
            aes.IV = FixedIv;

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs, Encoding.UTF8))
            {
                sw.Write(plainText);
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            using var aes = Aes.Create();
            aes.Key = KeyBytes;
            aes.IV = FixedIv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(Convert.FromBase64String(cipherText));
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs, Encoding.UTF8);

            return sr.ReadToEnd();
        }

        /// <summary>
        /// Decrypts the password inside a connection string if it is enclosed in ENC(...)
        /// or decrypts the whole connection string if it starts with ENC(...)
        /// If plain text, returns as-is without alteration.
        /// </summary>
        public static string DecryptConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return connectionString;

            connectionString = connectionString.Trim();

            // Case 1: Entire connection string is encrypted e.g. ENC(...)
            if (connectionString.StartsWith("ENC(", StringComparison.OrdinalIgnoreCase) && connectionString.EndsWith(")"))
            {
                var cipher = connectionString.Substring(4, connectionString.Length - 5);
                return Decrypt(cipher);
            }

            // Case 2: Only Password portion is encrypted e.g. Password=ENC(...)
            try
            {
                var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
                if (!string.IsNullOrEmpty(builder.Password) &&
                    builder.Password.StartsWith("ENC(", StringComparison.OrdinalIgnoreCase) &&
                    builder.Password.EndsWith(")"))
                {
                    var cipher = builder.Password.Substring(4, builder.Password.Length - 5);
                    builder.Password = Decrypt(cipher);
                    return builder.ConnectionString;
                }
            }
            catch
            {
                // Fallback regex/string replace if SqlConnectionStringBuilder cannot parse
                int encIdx = connectionString.IndexOf("ENC(", StringComparison.OrdinalIgnoreCase);
                if (encIdx >= 0)
                {
                    int closeIdx = connectionString.IndexOf(')', encIdx);
                    if (closeIdx > encIdx)
                    {
                        var cipher = connectionString.Substring(encIdx + 4, closeIdx - (encIdx + 4));
                        var plainPassword = Decrypt(cipher);
                        connectionString = connectionString.Substring(0, encIdx) + plainPassword + connectionString.Substring(closeIdx + 1);
                    }
                }
            }

            return connectionString;
        }
    }
}
