using System.Linq;
using Microsoft.EntityFrameworkCore;
using SmartPos.Lookups;

namespace SmartPos.EntityFrameworkCore.Seed.Host
{
    public class DefaultLookupsCreator
    {
        private readonly SmartPosDbContext _context;
        private readonly int? _tenantId;

        public DefaultLookupsCreator(SmartPosDbContext context, int? tenantId)
        {
            _context = context;
            _tenantId = tenantId;
        }

        public void Create()
        {
            foreach (var item in LookUpSeedData.Items)
            {
                Ensure(item);
            }

            _context.SaveChanges();
        }

        private void Ensure(LookUpSeedItem item)
        {
            var exists = _context.LookUps
                .IgnoreQueryFilters()
                .Any(x =>
                    x.TenantId == _tenantId
                    && x.Type == item.Type
                    && x.Name == item.Name
                    && !x.IsDeleted);

            if (exists)
            {
                return;
            }

            _context.LookUps.Add(new LookUp
            {
                TenantId = _tenantId,
                Type = item.Type,
                Name = item.Name,
                DisplayName = item.DisplayName,
                SortOrder = item.SortOrder,
                IsActive = true
            });
        }
    }
}
