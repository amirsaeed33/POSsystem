using System.Linq;
using Microsoft.EntityFrameworkCore;
using SmartPos.Authorization.Users;
using SmartPos.Branches;
using SmartPos.Lookups;

namespace SmartPos.EntityFrameworkCore.Seed.Tenants
{
    public class DefaultBranchCreator
    {
        private readonly SmartPosDbContext _context;
        private readonly int? _tenantId;

        public DefaultBranchCreator(SmartPosDbContext context, int? tenantId)
        {
            _context = context;
            _tenantId = tenantId;
        }

        public Branch Create()
        {
            var branch = _context.Branches.IgnoreQueryFilters()
                .FirstOrDefault(x => x.TenantId == _tenantId
                                     && x.Code == BranchConsts.DefaultBranchCode
                                     && !x.IsDeleted);

            if (branch == null)
            {
                branch = new Branch
                {
                    TenantId = _tenantId,
                    Name = BranchConsts.DefaultBranchName,
                    Code = BranchConsts.DefaultBranchCode,
                    StatusId = ResolveStatusId(BranchStatuses.Pending),
                    IsActive = true
                };
                _context.Branches.Add(branch);
                _context.SaveChanges();
            }

            AssignAdminUsers(branch.Id);
            return branch;
        }

        private int ResolveStatusId(string statusName)
        {
            var id = _context.LookUps.IgnoreQueryFilters()
                .Where(x =>
                    x.TenantId == null
                    && x.Type == LookUpTypes.BranchStatus
                    && x.Name == statusName
                    && !x.IsDeleted)
                .Select(x => x.Id)
                .FirstOrDefault();

            if (id == 0)
            {
                throw new System.InvalidOperationException(
                    $"Host BranchStatus lookup \"{statusName}\" is missing. Seed lookups before branches.");
            }

            return id;
        }

        private void AssignAdminUsers(int branchId)
        {
            var users = _context.Users.IgnoreQueryFilters()
                .Where(x => x.TenantId == _tenantId && x.BranchId == null && !x.IsDeleted)
                .ToList();

            foreach (var user in users)
            {
                user.BranchId = branchId;
            }

            if (users.Any())
            {
                _context.SaveChanges();
            }
        }
    }
}
