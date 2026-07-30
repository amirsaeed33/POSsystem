using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Abp.Dependency;
using Abp.UI;
using Microsoft.Extensions.Configuration;

namespace SmartPos.Authentication.EmailLogin
{
    public class EmailLoginCodeStore : ISingletonDependency
    {
        private readonly ConcurrentDictionary<string, Entry> _entries =
            new ConcurrentDictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);

        private readonly IConfiguration _configuration;

        public EmailLoginCodeStore(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public bool IsEnabled =>
            !string.Equals(
                _configuration["Authentication:EmailLogin:IsEnabled"],
                "false",
                StringComparison.OrdinalIgnoreCase);

        public int ExpirationMinutes
        {
            get
            {
                if (int.TryParse(_configuration["Authentication:EmailLogin:ExpirationMinutes"], out var minutes) &&
                    minutes > 0)
                {
                    return minutes;
                }

                return 5;
            }
        }

        public int ResendCooldownSeconds
        {
            get
            {
                if (int.TryParse(_configuration["Authentication:EmailLogin:ResendCooldownSeconds"], out var seconds) &&
                    seconds >= 0)
                {
                    return seconds;
                }

                return 60;
            }
        }

        public string CreateCode(int? tenantId, string emailAddress)
        {
            var key = BuildKey(tenantId, emailAddress);
            var now = DateTime.UtcNow;

            if (_entries.TryGetValue(key, out var existing) &&
                existing.LastSentAt.HasValue &&
                ResendCooldownSeconds > 0 &&
                now < existing.LastSentAt.Value.AddSeconds(ResendCooldownSeconds))
            {
                var waitSeconds = (int)Math.Ceiling(
                    (existing.LastSentAt.Value.AddSeconds(ResendCooldownSeconds) - now).TotalSeconds);
                throw new UserFriendlyException(
                    $"Please wait {Math.Max(waitSeconds, 1)} seconds before requesting another code.");
            }

            var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            var entry = new Entry
            {
                CodeHash = Hash(code),
                ExpiresAtUtc = now.AddMinutes(ExpirationMinutes),
                LastSentAt = now,
                FailedAttempts = 0
            };

            _entries[key] = entry;
            return code;
        }

        public void VerifyAndConsume(int? tenantId, string emailAddress, string code)
        {
            var key = BuildKey(tenantId, emailAddress);
            if (!_entries.TryGetValue(key, out var entry))
            {
                throw new UserFriendlyException("Invalid or expired sign-in code.");
            }

            if (DateTime.UtcNow > entry.ExpiresAtUtc)
            {
                _entries.TryRemove(key, out _);
                throw new UserFriendlyException("This sign-in code has expired. Please request a new one.");
            }

            if (string.IsNullOrWhiteSpace(code) ||
                !FixedTimeEquals(entry.CodeHash, Hash(code.Trim())))
            {
                entry.FailedAttempts++;
                if (entry.FailedAttempts >= 5)
                {
                    _entries.TryRemove(key, out _);
                    throw new UserFriendlyException("Too many invalid attempts. Please request a new code.");
                }

                throw new UserFriendlyException("Invalid or expired sign-in code.");
            }

            _entries.TryRemove(key, out _);
        }

        private static string BuildKey(int? tenantId, string emailAddress)
        {
            return $"{tenantId?.ToString() ?? "host"}:{NormalizeEmail(emailAddress)}";
        }

        private static string NormalizeEmail(string emailAddress)
        {
            return (emailAddress ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static string Hash(string value)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
                return Convert.ToBase64String(bytes);
            }
        }

        private static bool FixedTimeEquals(string left, string right)
        {
            var leftBytes = Encoding.UTF8.GetBytes(left ?? string.Empty);
            var rightBytes = Encoding.UTF8.GetBytes(right ?? string.Empty);
            return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
        }

        private sealed class Entry
        {
            public string CodeHash { get; set; }
            public DateTime ExpiresAtUtc { get; set; }
            public DateTime? LastSentAt { get; set; }
            public int FailedAttempts { get; set; }
        }
    }
}
