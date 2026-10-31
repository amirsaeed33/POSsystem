using System.Threading.Tasks;

namespace SmartPos.Caching
{
    public interface ISmartPosReadCacheInvalidator
    {
        Task ClearCatalogAsync();

        Task ClearModifiersAsync();

        Task ClearDeliveryAsync();

        Task ClearHeroSlidesAsync();

        Task ClearLookUpsAsync();

        Task ClearAllHotPathAsync();
    }
}
