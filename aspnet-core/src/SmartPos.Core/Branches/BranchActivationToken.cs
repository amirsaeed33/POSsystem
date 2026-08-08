using System;
using System.Security.Cryptography;
using System.Text;

namespace SmartPos.Branches
{
    public static class BranchActivationToken
    {
        public static string CreatePlainToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        public static string Hash(string plainToken)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(plainToken ?? string.Empty));
                return Convert.ToBase64String(bytes);
            }
        }

        public static bool FixedTimeEquals(string left, string right)
        {
            var leftBytes = Encoding.UTF8.GetBytes(left ?? string.Empty);
            var rightBytes = Encoding.UTF8.GetBytes(right ?? string.Empty);
            return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
        }
    }
}
