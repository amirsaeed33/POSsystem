using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SmartPos.Branches
{
    /// <summary>
    /// In-memory set of active custom storefront domains for CORS / quick checks.
    /// Populated from Branch.CustomDomain and refreshed when branches are saved.
    /// </summary>
    public static class StorefrontDomainRegistry
    {
        private static readonly ConcurrentDictionary<string, byte> Domains =
            new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

        public static void ReplaceAll(IEnumerable<string> domains)
        {
            var normalized = (domains ?? Enumerable.Empty<string>())
                .Select(StorefrontDomainHelper.Normalize)
                .Where(d => !string.IsNullOrEmpty(d))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var key in Domains.Keys.ToList())
            {
                if (!normalized.Contains(key, StringComparer.OrdinalIgnoreCase))
                {
                    Domains.TryRemove(key, out _);
                }
            }

            foreach (var d in normalized)
            {
                Domains[d] = 0;
            }
        }

        public static void Upsert(string domain)
        {
            var normalized = StorefrontDomainHelper.Normalize(domain);
            if (string.IsNullOrEmpty(normalized))
            {
                return;
            }

            Domains[normalized] = 0;
        }

        public static void Remove(string domain)
        {
            var normalized = StorefrontDomainHelper.Normalize(domain);
            if (string.IsNullOrEmpty(normalized))
            {
                return;
            }

            Domains.TryRemove(normalized, out _);
        }

        public static bool ContainsHost(string hostOrOrigin)
        {
            var normalized = StorefrontDomainHelper.Normalize(hostOrOrigin);
            return !string.IsNullOrEmpty(normalized) && Domains.ContainsKey(normalized);
        }
    }
}
