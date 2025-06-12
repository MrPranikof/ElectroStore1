using System;
using System.Security.Cryptography;
using System.Text;

namespace ElectroStore1
{
    public class Hash
    {
        private static string GenerateSalt()
        {
            byte[] saltBytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }

        public static string HashPassword(string password)
        {
            string salt = GenerateSalt();
            string saltedPassword = password + salt;

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));

                byte[] hashWithSaltBytes = new byte[hashBytes.Length + salt.Length];
                Buffer.BlockCopy(hashBytes, 0, hashWithSaltBytes, 0, hashBytes.Length);
                Buffer.BlockCopy(Encoding.UTF8.GetBytes(salt), 0, hashWithSaltBytes, hashBytes.Length, salt.Length);

                return Convert.ToBase64String(hashWithSaltBytes);
            }
        }

        public static bool VerifyPassword(string password, string hashedPassword)
        {
            byte[] hashWithSaltBytes = Convert.FromBase64String(hashedPassword);

            if (hashWithSaltBytes.Length < 32)
                return false;

            byte[] saltBytes = new byte[hashWithSaltBytes.Length - 32];
            Buffer.BlockCopy(hashWithSaltBytes, 32, saltBytes, 0, saltBytes.Length);
            string salt = Encoding.UTF8.GetString(saltBytes);

            string saltedPassword = password + salt;
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));

                // Сравниваем хеши
                for (int i = 0; i < 32; i++)
                {
                    if (hashBytes[i] != hashWithSaltBytes[i])
                        return false;
                }
                return true;
            }
        }
    }
}