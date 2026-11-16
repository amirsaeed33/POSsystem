using System;

namespace SmartPos.Branches
{
    /// <summary>Normalize and compare custom storefront hostnames.</summary>
    public static class StorefrontDomainHelper
    {
        public static string Normalize(string hostOrUrl)
        {
            if (string.IsNullOrWhiteSpace(hostOrUrl))
            {
                return null;
            }

            var value = hostOrUrl.Trim().ToLowerInvariant();
            if (value.StartsWith("https://", StringComparison.Ordinal))
            {
                value = value.Substring("https://".Length);
            }
            else if (value.StartsWith("http://", StringComparison.Ordinal))
            {
                value = value.Substring("http://".Length);
            }

            var slash = value.IndexOf('/');
            if (slash >= 0)
            {
                value = value.Substring(0, slash);
            }

            var colon = value.IndexOf(':');
            if (colon >= 0)
            {
                value = value.Substring(0, colon);
            }

            if (value.StartsWith("www.", StringComparison.Ordinal))
            {
                value = value.Substring(4);
            }

            value = value.Trim().TrimEnd('.');
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        public static bool IsPlatformHost(string host)
        {
            var normalized = Normalize(host);
            if (string.IsNullOrEmpty(normalized))
            {
                return true;
            }

            return normalized == "localhost"
                   || normalized == "127.0.0.1"
                   || normalized == "aljannatapp.com"
                   || normalized.EndsWith(".siteasp.net", StringComparison.Ordinal)
                   || normalized.EndsWith(".runasp.net", StringComparison.Ordinal)
                   || normalized.EndsWith(".ngrok-free.dev", StringComparison.Ordinal)
                   || normalized.EndsWith(".ngrok-free.app", StringComparison.Ordinal)
                   || normalized.EndsWith(".ngrok.io", StringComparison.Ordinal);
        }

        public static bool HostsMatch(string left, string right)
        {
            var a = Normalize(left);
            var b = Normalize(right);
            return !string.IsNullOrEmpty(a)
                   && !string.IsNullOrEmpty(b)
                   && string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Match host against stored domain, including www alias.</summary>
        public static bool HostMatchesDomain(string requestHost, string customDomain)
        {
            var host = Normalize(requestHost);
            var domain = Normalize(customDomain);
            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(domain))
            {
                return false;
            }

            return string.Equals(host, domain, StringComparison.OrdinalIgnoreCase);
        }
    }
}
