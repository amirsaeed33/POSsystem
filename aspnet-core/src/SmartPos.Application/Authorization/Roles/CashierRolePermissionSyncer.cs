using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Dependency;
using Abp.Domain.Uow;
using Microsoft.EntityFrameworkCore;
using SmartPos.Authorization.Roles;

namespace SmartPos.Authorization.Roles
{
    /// <summary>
    /// Ensures the tenant "Cashier" role has Dine Hub + Staff + Payroll access
    /// (and no broader admin modules). Safe to call on login / role list.
    /// </summary>
    public class CashierRolePermissionSyncer : ITransientDependency
    {
        public static readonly string[] CashierPermissionNames =
        {
            PermissionNames.Pages_Pos,
            PermissionNames.Pages_Pos_Create,
            PermissionNames.Pages_Pos_Edit,
            PermissionNames.Pages_DiningTables,
            PermissionNames.Pages_Sales,
            PermissionNames.Pages_Sales_Create,
            PermissionNames.Pages_Staff,
            PermissionNames.Pages_StaffPayroll,
            PermissionNames.Pages_StaffPayroll_Create,
            PermissionNames.Pages_StaffPayroll_Edit,
        };

        private readonly RoleManager _roleManager;
        private readonly IPermissionManager _permissionManager;
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        public CashierRolePermissionSyncer(
            RoleManager roleManager,
            IPermissionManager permissionManager,
            IUnitOfWorkManager unitOfWorkManager)
        {
            _roleManager = roleManager;
            _permissionManager = permissionManager;
            _unitOfWorkManager = unitOfWorkManager;
        }

        public async Task EnsureAsync(int? tenantId)
        {
            if (!tenantId.HasValue || _unitOfWorkManager.Current == null)
            {
                return;
            }

            using (_unitOfWorkManager.Current.SetTenantId(tenantId))
            {
                var role = await _roleManager.Roles
                    .FirstOrDefaultAsync(r =>
                        r.TenantId == tenantId &&
                        (r.Name == StaticRoleNames.Tenants.Cashier
                         || r.NormalizedName == StaticRoleNames.Tenants.Cashier.ToUpperInvariant()));

                if (role == null)
                {
                    role = new Role(tenantId, StaticRoleNames.Tenants.Cashier, StaticRoleNames.Tenants.Cashier)
                    {
                        IsStatic = false,
                        Description = "Dine Hub, Staff, and Payroll. Can add items after KOT but cannot deduct."
                    };
                    var createResult = await _roleManager.CreateAsync(role);
                    if (!createResult.Succeeded)
                    {
                        return;
                    }
                }
                else if (string.IsNullOrWhiteSpace(role.Description)
                         || role.Description.IndexOf("payment from rider", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    role.Description =
                        "Dine Hub, Staff, and Payroll. Can add items after KOT but cannot deduct.";
                    await _roleManager.UpdateAsync(role);
                }

                var desired = _permissionManager
                    .GetAllPermissions()
                    .Where(p => CashierPermissionNames.Contains(p.Name))
                    .ToList();

                if (desired.Count == 0)
                {
                    return;
                }

                var current = (await _roleManager.GetGrantedPermissionsAsync(role))
                    .Select(p => p.Name)
                    .OrderBy(n => n)
                    .ToList();
                var desiredNames = desired.Select(p => p.Name).OrderBy(n => n).ToList();

                if (!current.SequenceEqual(desiredNames))
                {
                    await _roleManager.SetGrantedPermissionsAsync(role, desired);
                }
            }
        }
    }
}
