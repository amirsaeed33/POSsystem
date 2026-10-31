using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Runtime.Caching;

namespace SmartPos.Caching
{
    public class SmartPosReadCacheInvalidator : ISmartPosReadCacheInvalidator, ITransientDependency
    {
        private readonly ICacheManager _cacheManager;

        public SmartPosReadCacheInvalidator(ICacheManager cacheManager)
        {
            _cacheManager = cacheManager;
        }

        public Task ClearCatalogAsync() => SmartPosAppCaches.ClearCatalogCachesAsync(_cacheManager);

        public Task ClearModifiersAsync() => SmartPosAppCaches.ClearAsync(_cacheManager, SmartPosAppCaches.ProductModifiers);

        public Task ClearDeliveryAsync() =>
            SmartPosAppCaches.ClearManyAsync(
                _cacheManager,
                SmartPosAppCaches.DeliveryAreas,
                SmartPosAppCaches.OnlineStoreHeader);

        public Task ClearHeroSlidesAsync() => SmartPosAppCaches.ClearAsync(_cacheManager, SmartPosAppCaches.HeroSlides);

        public Task ClearLookUpsAsync() => SmartPosAppCaches.ClearAsync(_cacheManager, SmartPosAppCaches.LookUpsByType);

        public Task ClearAllHotPathAsync() =>
            SmartPosAppCaches.ClearManyAsync(
                _cacheManager,
                SmartPosAppCaches.PosProducts,
                SmartPosAppCaches.TopSellingProducts,
                SmartPosAppCaches.OnlineCatalog,
                SmartPosAppCaches.OnlineStoreHeader,
                SmartPosAppCaches.ProductModifiers,
                SmartPosAppCaches.DealByProduct,
                SmartPosAppCaches.DeliveryAreas,
                SmartPosAppCaches.HeroSlides,
                SmartPosAppCaches.LookUpsByType);
    }
}