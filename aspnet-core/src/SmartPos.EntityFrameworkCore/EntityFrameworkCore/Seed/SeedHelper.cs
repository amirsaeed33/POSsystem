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
                // Host seed only — do not auto-create a Default tenant or admin@defaulttenant.com.
                new InitialHostDbBuilder(context).Create();
                // Lookups (incl. host BranchStatus) before tenant branches that FK StatusId.
                new DefaultLookupsCreator(context, null).Create();
                // Host admin has no own location — locations belong to businesses only.
                ClearHostUserBranchAssignments(context);
                new DefaultEmailTemplatesCreator(context, null).Create();
                new DefaultHostCatalogCreator(context).Create();
                new BakeryGeneralStoreDemoDataCreator(context, null).Create();

                // Keep tenant admin roles/permissions in sync for tenants created via signup/host UI.
                // Do not create default admin users here.
                var tenantIds = context.Tenants.IgnoreQueryFilters().Select(t => t.Id).ToList();
                foreach (var tenantId in tenantIds)
                {
                    new TenantRoleAndUserBuilder(context, tenantId).Create(createAdminUser: false);
                    new DefaultLookupsCreator(context, tenantId).Create();
                }
            }
            finally
            {
                context.SuppressAutoSetTenantId = false;
            }
        }

        private static void ClearHostUserBranchAssignments(SmartPosDbContext context)
        {
            var hostUsers = context.Users.IgnoreQueryFilters()
                .Where(x => x.TenantId == null && x.BranchId != null && !x.IsDeleted)
                .ToList();

            if (!hostUsers.Any())
            {
                return;
            }

            foreach (var user in hostUsers)
            {
                user.BranchId = null;
            }

            context.SaveChanges();
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
