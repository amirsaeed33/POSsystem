using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using SmartPos.Lookups;

namespace SmartPos.Branches
{
    public class BranchStatusLookup : ITransientDependency
    {
        private readonly IRepository<LookUp> _lookUpRepository;
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        public BranchStatusLookup(
            IRepository<LookUp> lookUpRepository,
            IUnitOfWorkManager unitOfWorkManager)
        {
            _lookUpRepository = lookUpRepository;
            _unitOfWorkManager = unitOfWorkManager;
        }

        /// <summary>Resolves host-scoped BranchStatus LookUp Id by Name.</summary>
        public async Task<int> GetIdAsync(string statusName)
        {
            using (_unitOfWorkManager.Current.DisableFilter(AbpDataFilters.MayHaveTenant))
            {
                var id = await _lookUpRepository.GetAll()
                    .Where(x =>
                        x.TenantId == null
                        && x.Type == LookUpTypes.BranchStatus
                        && x.Name == statusName
                        && x.IsActive)
                    .Select(x => x.Id)
                    .FirstOrDefaultAsync();

                if (id == 0)
                {
                    throw new UserFriendlyException(
                        $"Branch status \"{statusName}\" is not configured in lookups.");
                }

                return id;
            }
        }

        public async Task<LookUp> GetAsync(int statusId)
        {
            using (_unitOfWorkManager.Current.DisableFilter(AbpDataFilters.MayHaveTenant))
            {
                var lookUp = await _lookUpRepository.GetAll()
                    .FirstOrDefaultAsync(x =>
                        x.Id == statusId
                        && x.Type == LookUpTypes.BranchStatus
                        && x.IsActive);

                if (lookUp == null)
                {
                    throw new UserFriendlyException("Invalid branch status.");
                }

                return lookUp;
            }
        }

        public async Task EnsureValidStatusIdAsync(int statusId)
        {
            await GetAsync(statusId);
        }
    }
}
