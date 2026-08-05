using System;
using System.Linq;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.EntityFrameworkCore.Uow;
using Abp.MultiTenancy;
using SmartPos.EntityFrameworkCore.Seed.Host;
using SmartPos.EntityFrameworkCore.Seed.Tenants;

namespace SmartPos.EntityFrameworkCore.Seed
{
    public static class SeedHelper
    {
        public static void SeedHostDb(IIocResolver iocResolver)
        {
            WithDbContext<SmartPosDbContext>(iocResolver, SeedHostDb);
        }

        public static void SeedHostDb(SmartPosDbContext context)
        {
            // Must reset — a stuck true flag skips TenantId/CreatorUserId on later inserts.
            context.SuppressAutoSetTenantId = true;
            try
            {
                // Host seed
                new InitialHostDbBuilder(context).Create();

                // Default tenant seed (in host database).
                new DefaultTenantBuilder(context).Create();
                var tenantIds = context.Tenants.IgnoreQueryFilters().Select(t => t.Id).ToList();
                foreach (var tenantId in tenantIds)
                {
                    new TenantRoleAndUserBuilder(context, tenantId).Create();
                }
                new DefaultBranchCreator(context, null).Create();
                new DefaultBranchCreator(context, 1).Create();
                new DefaultSystemAccountsCreator(context, 1).Create();
                new DefaultEmailTemplatesCreator(context, null).Create();
                new DefaultEmailTemplatesCreator(context, 1).Create();
                new DefaultLookupsCreator(context, null).Create();
                foreach (var tenantId in tenantIds)
                {
                    new DefaultLookupsCreator(context, tenantId).Create();
                }
                new Host.BakeryGeneralStoreDemoDataCreator(context, null).Create();
                new Host.BakeryGeneralStoreDemoDataCreator(context, 1).Create();
            }
            finally
            {
                context.SuppressAutoSetTenantId = false;
            }
        }

        private static void WithDbContext<TDbContext>(IIocResolver iocResolver, Action<TDbContext> contextAction)
            where TDbContext : DbContext
        {
            using (var uowManager = iocResolver.ResolveAsDisposable<IUnitOfWorkManager>())
            {
                using (var uow = uowManager.Object.Begin(TransactionScopeOption.Suppress))
                {
                    var context = uowManager.Object.Current.GetDbContext<TDbContext>(MultiTenancySides.Host);

                    contextAction(context);

                    uow.Complete();
                }
            }
        }
    }
}
