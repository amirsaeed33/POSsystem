using System;
using System.Threading.Tasks;
using Abp.Runtime.Caching;

namespace SmartPos.Caching
{
    /// <summary>
    /// Named application caches + helpers. Keys must include Tenant/Branch where relevant.
    /// </summary>
    public static class SmartPosAppCaches
    {
        public const string PosProducts = "PosProductsCache";
        public const string TopSellingProducts = "TopSellingProductsCache";
        public const string OnlineCatalog = "OnlineCatalogCache";
        public const string OnlineStoreHeader = "OnlineStoreHeaderCache";
        public const string ProductModifiers = "ProductModifiersCache";
        public const string DealByProduct = "DealByProductCache";
        public const string DeliveryAreas = "DeliveryAreasCache";
        public const string HeroSlides = "HeroSlidesCache";
        public const string LookUpsByType = "LookUpsByTypeCache";

        public static string TenantBranchKey(int? tenantId, int? branchId, string suffix = null)
        {
            var key = $"T_{tenantId ?? 0}_B_{branchId?.ToString() ?? "All"}";
            return string.IsNullOrWhiteSpace(suffix) ? key : $"{key}_{suffix}";
        }

        public static string BranchKey(int branchId, string suffix = null)
        {
            var key = $"B_{branchId}";
            return string.IsNullOrWhiteSpace(suffix) ? key : $"{key}_{suffix}";
        }

        public static string ProductKey(int productId) => $"P_{productId}";

        public static ITypedCache<string, T> Typed<T>(ICacheManager cacheManager, string name, TimeSpan slidingExpire)
        {
            var cache = cacheManager.GetCache<string, T>(name);
            cache.DefaultSlidingExpireTime = slidingExpire;
            return cache;
        }

        public static Task ClearAsync(ICacheManager cacheManager, string name)
        {
            return cacheManager.GetCache(name).ClearAsync();
        }

        public static async Task ClearManyAsync(ICacheManager cacheManager, params string[] names)
        {
            if (names == null || names.Length == 0)
            {
                return;
            }

            foreach (var name in names)
            {
                await cacheManager.GetCache(name).ClearAsync();
            }
        }

        /// <summary>
        /// Clears catalog/menu caches that depend on products, prices, deals, or categories.
        /// </summary>
        public static Task ClearCatalogCachesAsync(ICacheManager cacheManager)
        {
            return ClearManyAsync(
                cacheManager,
                PosProducts,
                TopSellingProducts,
                OnlineCatalog,
                OnlineStoreHeader,
                DealByProduct);
        }
    }
}
