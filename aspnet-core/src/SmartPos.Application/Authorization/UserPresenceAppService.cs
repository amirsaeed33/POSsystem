using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Timing;
using Microsoft.EntityFrameworkCore;
using SmartPos.Authorization.Users;
using SmartPos.Branches;
using SmartPos.MultiTenancy;

namespace SmartPos.Authorization
{
    public class OnlineUserDto
    {
        public long UserId { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string RoleNames { get; set; }
        public int? BranchId { get; set; }
        public string BranchName { get; set; }
        public int? TenantId { get; set; }
        public string TenancyName { get; set; }
        public DateTime LoginTime { get; set; }
        public DateTime LastSeen { get; set; }
    }

    public interface IUserPresenceAppService : IApplicationService
    {
        Task PingAsync();

        Task SignOutAsync();

        Task<List<OnlineUserDto>> GetOnlineAsync();
    }

    /// <summary>
    /// Tracks who currently has the app open. Host sees every business.
    /// A branch login sees only that branch.
    /// </summary>
    [AbpAuthorize]
    public class UserPresenceAppService : ApplicationService, IUserPresenceAppService
    {
        public static readonly TimeSpan OnlineWindow = TimeSpan.FromMinutes(3);

        private readonly IRepository<UserPresence, long> _presenceRepository;
        private readonly IRepository<User, long> _userRepository;
        private readonly IRepository<Branch> _branchRepository;
        private readonly IRepository<Tenant> _tenantRepository;
        private readonly UserManager _userManager;
        private readonly IBranchContext _branchContext;

        public UserPresenceAppService(
            IRepository<UserPresence, long> presenceRepository,
            IRepository<User, long> userRepository,
            IRepository<Branch> branchRepository,
            IRepository<Tenant> tenantRepository,
            UserManager userManager,
            IBranchContext branchContext)
        {
            _presenceRepository = presenceRepository;
            _userRepository = userRepository;
            _branchRepository = branchRepository;
            _tenantRepository = tenantRepository;
            _userManager = userManager;
            _branchContext = branchContext;
        }

        public async Task PingAsync()
        {
            if (!AbpSession.UserId.HasValue)
            {
                return;
            }

            var user = await _userRepository.FirstOrDefaultAsync(AbpSession.UserId.Value);
            if (user == null || !user.IsActive)
            {
                return;
            }

            var now = Clock.Now;
            var row = await FindPresenceAsync(user.Id);
            var isNewSession = row == null
                || row.SignedOutTime.HasValue
                || row.LastSeen < now.Subtract(OnlineWindow);

            if (row == null)
            {
                row = new UserPresence
                {
                    UserId = user.Id,
                    LoginTime = now,
                };
                await FillSnapshotAsync(row, user);
                row.LastSeen = now;
                row.SignedOutTime = null;
                await _presenceRepository.InsertAsync(row);
                return;
            }

            await FillSnapshotAsync(row, user);
            if (isNewSession)
            {
                row.LoginTime = now;
            }

            row.LastSeen = now;
            row.SignedOutTime = null;
            await _presenceRepository.UpdateAsync(row);
        }

        public async Task SignOutAsync()
        {
            if (!AbpSession.UserId.HasValue)
            {
                return;
            }

            var row = await FindPresenceAsync(AbpSession.UserId.Value);
            if (row == null)
            {
                return;
            }

            row.SignedOutTime = Clock.Now;
            row.LastSeen = row.SignedOutTime.Value;
            await _presenceRepository.UpdateAsync(row);
        }

        [AbpAuthorize(PermissionNames.Pages_Users)]
        public async Task<List<OnlineUserDto>> GetOnlineAsync()
        {
            var cutoff = Clock.Now.Subtract(OnlineWindow);
            var isHost = !AbpSession.TenantId.HasValue;

            using (isHost
                ? CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant)
                : null)
            {
                var query = _presenceRepository.GetAll()
                    .Where(x => x.SignedOutTime == null && x.LastSeen >= cutoff);

                if (!isHost)
                {
                    var branchId = await ResolveViewerBranchIdAsync();
                    if (branchId.HasValue)
                    {
                        query = query.Where(x => x.BranchId == branchId.Value);
                    }
                }

                var rows = await query
                    .OrderByDescending(x => x.LastSeen)
                    .ToListAsync();

                return rows.Select(x => new OnlineUserDto
                {
                    UserId = x.UserId,
                    UserName = x.UserName,
                    FullName = x.FullName,
                    RoleNames = x.RoleNames,
                    BranchId = x.BranchId,
                    BranchName = x.BranchName,
                    TenantId = x.TenantId,
                    TenancyName = x.TenancyName,
                    LoginTime = x.LoginTime,
                    LastSeen = x.LastSeen,
                }).ToList();
            }
        }

        private async Task<UserPresence> FindPresenceAsync(long userId)
        {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
            {
                return await _presenceRepository.FirstOrDefaultAsync(x => x.UserId == userId);
            }
        }

        private async Task<int?> ResolveViewerBranchIdAsync()
        {
            if (_branchContext.BranchId.HasValue)
            {
                return _branchContext.BranchId;
            }

            if (!AbpSession.UserId.HasValue)
            {
                return null;
            }

            var me = await _userRepository.FirstOrDefaultAsync(AbpSession.UserId.Value);
            return me?.BranchId;
        }

        private async Task FillSnapshotAsync(UserPresence row, User user)
        {
            row.TenantId = user.TenantId;
            row.UserId = user.Id;
            row.BranchId = user.BranchId;
            row.UserName = user.UserName;
            row.FullName = ((user.Name ?? string.Empty) + " " + (user.Surname ?? string.Empty)).Trim();
            if (string.IsNullOrWhiteSpace(row.FullName))
            {
                row.FullName = user.UserName;
            }

            row.BranchName = null;
            if (user.BranchId.HasValue)
            {
                var branch = await _branchRepository.FirstOrDefaultAsync(user.BranchId.Value);
                row.BranchName = branch?.Name;
            }

            row.TenancyName = user.TenantId.HasValue ? null : "Host";
            if (user.TenantId.HasValue)
            {
                using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
                {
                    var tenant = await _tenantRepository.FirstOrDefaultAsync(user.TenantId.Value);
                    row.TenancyName = tenant?.TenancyName ?? tenant?.Name;
                }
            }

            var roles = await _userManager.GetRolesAsync(user);
            row.RoleNames = roles == null || roles.Count == 0
                ? null
                : string.Join(", ", roles);
        }
    }
}
