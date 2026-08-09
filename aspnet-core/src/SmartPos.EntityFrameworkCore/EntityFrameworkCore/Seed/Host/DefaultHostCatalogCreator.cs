using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SmartPos.HostCatalog;

namespace SmartPos.EntityFrameworkCore.Seed.Host
{
    public class DefaultHostCatalogCreator
    {
        private readonly SmartPosDbContext _context;

        public DefaultHostCatalogCreator(SmartPosDbContext context)
        {
            _context = context;
        }

        public void Create()
        {
            var companyTypeIds = new Dictionary<string, int>();

            foreach (var item in HostCatalogSeedData.Items)
            {
                if (item.Type == HostCatalogItemTypes.CompanyType)
                {
                    var id = EnsureCompanyType(item.Name);
                    companyTypeIds[item.Name] = id;
                }
            }

            foreach (var item in HostCatalogSeedData.Items)
            {
                if (item.Type == HostCatalogItemTypes.CompanyType)
                {
                    continue;
                }

                if (!companyTypeIds.TryGetValue(item.CompanyTypeName, out var companyTypeId))
                {
                    continue;
                }

                EnsureChild(item.Type, companyTypeId, item.Name, item.Symbol);
            }

            _context.SaveChanges();
        }

        private int EnsureCompanyType(string name)
        {
            var existing = _context.HostCatalogItems
                .IgnoreQueryFilters()
                .FirstOrDefault(x =>
                    x.Type == HostCatalogItemTypes.CompanyType
                    && x.Name == name
                    && x.CompanyTypeId == null
                    && !x.IsDeleted);

            if (existing != null)
            {
                return existing.Id;
            }

            var entity = new HostCatalogItem
            {
                Type = HostCatalogItemTypes.CompanyType,
                CompanyTypeId = null,
                Name = name,
                IsActive = true
            };
            _context.HostCatalogItems.Add(entity);
            _context.SaveChanges();
            return entity.Id;
        }

        private void EnsureChild(string type, int companyTypeId, string name, string symbol)
        {
            var exists = _context.HostCatalogItems
                .IgnoreQueryFilters()
                .Any(x =>
                    x.Type == type
                    && x.CompanyTypeId == companyTypeId
                    && x.Name == name
                    && !x.IsDeleted);

            if (exists)
            {
                return;
            }

            _context.HostCatalogItems.Add(new HostCatalogItem
            {
                Type = type,
                CompanyTypeId = companyTypeId,
                Name = name,
                Symbol = symbol,
                IsActive = true
            });
        }
    }
}
