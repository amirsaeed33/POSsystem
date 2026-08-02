using System.Linq;
using Microsoft.EntityFrameworkCore;
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
                                     && x.Code == Branch.DefaultBranchCode
                                     && !x.IsDeleted);

            if (branch == null)
            {
                branch = new Branch
                {
                    TenantId = _tenantId,
                    Name = Branch.DefaultBranchName,
                    Code = Branch.DefaultBranchCode,
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
            var adminUsers = _context.Users.IgnoreQueryFilters()
                .Where(x => x.TenantId == _tenantId && !x.IsDeleted)
                .ToList();

            foreach (var user in adminUsers)
            {
                var exists = _context.UserBranches.IgnoreQueryFilters()
                    .Any(x => x.TenantId == _tenantId && x.UserId == user.Id && x.BranchId == branchId);
                if (exists)
                {
                    continue;
                }

                _context.UserBranches.Add(new UserBranch
                {
                    TenantId = _tenantId,
                    UserId = user.Id,
                    BranchId = branchId
                });
            }

            _context.SaveChanges();
        }
    }
}
