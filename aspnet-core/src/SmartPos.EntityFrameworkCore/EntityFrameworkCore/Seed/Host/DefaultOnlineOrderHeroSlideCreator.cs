using System.Linq;
using Microsoft.EntityFrameworkCore;
using SmartPos.Branches;
using SmartPos.Orders;

namespace SmartPos.EntityFrameworkCore.Seed.Host
{
    /// <summary>
    /// Seeds default online-order hero banners (with local default images)
    /// for restaurant branches that do not have any slides yet.
    /// </summary>
    public class DefaultOnlineOrderHeroSlideCreator
    {
        private readonly SmartPosDbContext _context;

        public DefaultOnlineOrderHeroSlideCreator(SmartPosDbContext context)
        {
            _context = context;
        }

        public void Create()
        {
            var restaurantBranches = _context.Branches
                .IgnoreQueryFilters()
                .Where(b => !b.IsDeleted && b.BusinessType == BusinessType.Restaurant)
                .Select(b => new { b.Id, b.TenantId })
                .ToList();

            if (!restaurantBranches.Any())
            {
                return;
            }

            var branchIdsWithSlides = _context.OnlineOrderHeroSlides
                .IgnoreQueryFilters()
                .Where(s => !s.IsDeleted)
                .Select(s => s.BranchId)
                .Distinct()
                .ToHashSet();

            var blueprints = OnlineOrderHeroSlideDefaults.GetBlueprints();
            var added = false;

            foreach (var branch in restaurantBranches)
            {
                if (branchIdsWithSlides.Contains(branch.Id))
                {
                    continue;
                }

                foreach (var bp in blueprints)
                {
                    _context.OnlineOrderHeroSlides.Add(
                        OnlineOrderHeroSlideDefaults.CreateEntity(branch.Id, branch.TenantId, bp));
                }

                added = true;
            }

            if (added)
            {
                _context.SaveChanges();
            }
        }
    }
}
