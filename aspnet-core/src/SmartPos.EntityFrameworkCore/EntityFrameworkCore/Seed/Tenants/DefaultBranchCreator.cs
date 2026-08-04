using System.Linq;
using Microsoft.EntityFrameworkCore;
using SmartPos.Authorization.Users;
using SmartPos.Branches;

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
                    IsActive = true,
                    IsDefault = true
                };
                _context.Branches.Add(branch);
                _context.SaveChanges();
            }

            AssignAdminUsers(branch.Id);
            return branch;
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
