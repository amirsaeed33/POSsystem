using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Auditing;
using Abp.Domain.Repositories;
using SmartPos.Branches;
using SmartPos.Sessions.Dto;

namespace SmartPos.Sessions
{
    public class SessionAppService : SmartPosAppServiceBase, ISessionAppService
    {
        private readonly IRepository<Branch> _branchRepository;

        public SessionAppService(IRepository<Branch> branchRepository)
        {
            _branchRepository = branchRepository;
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
                output.Tenant = ObjectMapper.Map<TenantLoginInfoDto>(await GetCurrentTenantAsync());
            }

            if (AbpSession.UserId.HasValue)
            {
                var user = await GetCurrentUserAsync();
                output.User = ObjectMapper.Map<UserLoginInfoDto>(user);

                if (user.BranchId.HasValue)
                {
                    var branch = await _branchRepository.FirstOrDefaultAsync(user.BranchId.Value);
                    output.User.BranchName = branch?.Name;
                }
            }

            return output;
        }
    }
}
