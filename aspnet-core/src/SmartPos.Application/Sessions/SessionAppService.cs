using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Auditing;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using SmartPos.Authorization;
using SmartPos.Authorization.Roles;
using SmartPos.Branches;
using SmartPos.Sessions.Dto;
using SmartPos.Staffs;

namespace SmartPos.Sessions
{
    public class SessionAppService : SmartPosAppServiceBase, ISessionAppService
    {
        private readonly IRepository<Branch> _branchRepository;
        private readonly IRepository<Staff> _staffRepository;
        private readonly RoleManager _roleManager;

        public SessionAppService(
            IRepository<Branch> branchRepository,
            IRepository<Staff> staffRepository,
            RoleManager roleManager)
        {
            _branchRepository = branchRepository;
            _staffRepository = staffRepository;
            _roleManager = roleManager;
        }

        [DisableAuditing]
        public async Task<GetCurrentLoginInformationsOutput> GetCurrentLoginInformations()
        {
            var output = new GetCurrentLoginInformationsOutput
            {
                Application = new ApplicationInfoDto
                {
                    Version = AppVersionHelper.Version,
                    ReleaseDate = AppVersionHelper.ReleaseDate,
                    Features = new Dictionary<string, bool>()
                }
            };

            if (AbpSession.TenantId.HasValue)
            {
                var tenant = await GetCurrentTenantAsync();
                output.Tenant = tenant != null ? new TenantLoginInfoDto
                {
                    Id = tenant.Id,
                    TenancyName = tenant.TenancyName,
                    Name = tenant.Name
                } : null;
            }

            if (AbpSession.UserId.HasValue)
            {
                var user = await GetCurrentUserAsync();
                output.User = new UserLoginInfoDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Surname = user.Surname,
                    UserName = user.UserName,
                    EmailAddress = user.EmailAddress,
                    UserImageUrl = user.UserImageUrl,
                    BranchId = user.BranchId
                };

                if (user.BranchId.HasValue)
                {
                    var branch = await _branchRepository.FirstOrDefaultAsync(user.BranchId.Value);
                    output.User.BranchName = branch?.Name;
                    output.User.BranchBusinessType = branch != null ? (int)branch.BusinessType : null;
                }

                var userRoles = await UserManager.GetRolesAsync(user);
                output.User.Roles = userRoles.ToList();

                var staff = await _staffRepository.FirstOrDefaultAsync(s => s.UserId == user.Id && !s.IsDeleted);
                if (staff != null)
                {
                    output.User.RiderStaffId = staff.Id;
                    output.User.Designation = staff.Designation;

                    var isRiderDesignation = staff.Designation != null &&
                        (staff.Designation.ToLower().Contains("rider") || staff.Designation.ToLower().Contains("delivery"));

                    var isWaiterDesignation = staff.Designation != null &&
                        (staff.Designation.ToLower().Contains("waiter") || staff.Designation.ToLower().Contains("server"));

                    // Restaurant branch specific roles
                    if (output.User.BranchBusinessType == (int)BusinessType.Restaurant)
                    {
                        if (isRiderDesignation)
                        {
                            output.User.IsDeliveryRider = true;

                            // Ensure Delivery Rider role is assigned to the user
                            if (!userRoles.Contains(StaticRoleNames.Tenants.DeliveryRider))
                            {
                                var role = await _roleManager.Roles.FirstOrDefaultAsync(r => r.TenantId == user.TenantId && r.Name == StaticRoleNames.Tenants.DeliveryRider);
                                if (role == null)
                                {
                                    role = new Role(user.TenantId, StaticRoleNames.Tenants.DeliveryRider, StaticRoleNames.Tenants.DeliveryRider)
                                    {
                                        IsStatic = false
                                    };
                                    await _roleManager.CreateAsync(role);
                                    await _roleManager.SetGrantedPermissionsAsync(role, new[]
                                    {
                                        new Permission(PermissionNames.Pages_Rider),
                                        new Permission(PermissionNames.Pages_Rider_Dashboard),
                                        new Permission(PermissionNames.Pages_Rider_OrderHistory)
                                    });
                                }
                                await UserManager.AddToRoleAsync(user, role.Name);
                                output.User.Roles.Add(role.Name);
                            }
                        }
                        else if (isWaiterDesignation || userRoles.Contains(StaticRoleNames.Tenants.Waiter))
                        {
                            output.User.IsWaiter = true;

                            if (!userRoles.Contains(StaticRoleNames.Tenants.Waiter))
                            {
                                var role = await _roleManager.Roles.FirstOrDefaultAsync(r => r.TenantId == user.TenantId && r.Name == StaticRoleNames.Tenants.Waiter);
                                if (role == null)
                                {
                                    role = new Role(user.TenantId, StaticRoleNames.Tenants.Waiter, StaticRoleNames.Tenants.Waiter)
                                    {
                                        IsStatic = false
                                    };
                                    await _roleManager.CreateAsync(role);
                                    await _roleManager.SetGrantedPermissionsAsync(role, new[]
                                    {
                                        new Permission(PermissionNames.Pages_Pos),
                                        new Permission(PermissionNames.Pages_DiningTables)
                                    });
                                }
                                await UserManager.AddToRoleAsync(user, role.Name);
                                output.User.Roles.Add(role.Name);
                            }
                        }
                    }
                }
                else if (output.User.BranchBusinessType == (int)BusinessType.Restaurant && userRoles.Contains(StaticRoleNames.Tenants.Waiter))
                {
                    output.User.IsWaiter = true;
                }
            }

            return output;
        }
    }
}
