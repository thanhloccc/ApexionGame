using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using EncosyTower.Logging;

namespace EncosyTower.Encryption
{
    public sealed class AesEncryption : EncryptionBase
    {
        /// <param name="iterations">
        /// The number of iterations for the key derivation function.
        /// <br/>
        /// Default is 10_000, which is a recommended value by security experts.
        /// However, you can adjust this value based on your security requirements and performance considerations.
        /// </param>
        public AesEncryption(
              [NotNull] string password
            , [NotNull] string saltKey
            , [NotNull] ILogger logger
            , int iterations = 10_000
        )
            : base(logger)
        {
            using var rfc2898 = new Rfc2898DeriveBytes(
                  password
                , Encoding.UTF8.GetBytes(saltKey)
                , iterations
                , HashAlgorithmName.SHA256
            );

            using var algorithm = new AesManaged {
                Key = rfc2898.GetBytes(16),
                IV = rfc2898.GetBytes(16),
            };

            Initialize(algorithm.CreateEncryptor(), algorithm.CreateDecryptor());
        }
    }
}
