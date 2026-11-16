using System;
using Microsoft.Extensions.Configuration;

namespace SmartPos.Branches
{
    /// <summary>
    /// Build public storefront roots for emails / redirects.
    /// Prefers the branch custom domain, then App:ClientRootAddress.
    /// </summary>
    public static class StorefrontUrlHelper
    {
        public static string GetPublicRoot(Branch branch, IConfiguration configuration)
        {
            var custom = StorefrontDomainHelper.Normalize(branch?.CustomDomain);
            if (!string.IsNullOrEmpty(custom))
            {
                return $"https://{custom}/";
            }

            var clientRoot = (configuration?["App:ClientRootAddress"] ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(clientRoot))
            {
                clientRoot = "https://aljannatapp.com/";
            }

            if (!clientRoot.EndsWith("/", StringComparison.Ordinal))
            {
                clientRoot += "/";
            }

            return clientRoot;
        }

        public static string GetPublicRootForHost(string hostOrOrigin, IConfiguration configuration)
        {
            var custom = StorefrontDomainHelper.Normalize(hostOrOrigin);
            if (!string.IsNullOrEmpty(custom) && !StorefrontDomainHelper.IsPlatformHost(custom))
            {
                return $"https://{custom}/";
            }

            return GetPublicRoot(null, configuration);
        }

        public static string BuildOnlineOrderUrl(
            Branch branch,
            IConfiguration configuration,
            int? branchId = null)
        {
            var root = GetPublicRoot(branch, configuration);
            var id = branchId ?? branch?.Id ?? 0;
            if (id > 0 && string.IsNullOrEmpty(StorefrontDomainHelper.Normalize(branch?.CustomDomain)))
            {
                return $"{root}online-order?branchId={id}";
            }

            return $"{root}online-order";
        }
    }
}
