using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Runtime.Session;

namespace SmartPos.Staffs
{
    public class StaffHistoryWriter : ITransientDependency
    {
        private readonly IRepository<StaffHistory> _historyRepository;
        private readonly IAbpSession _abpSession;

        public StaffHistoryWriter(
            IRepository<StaffHistory> historyRepository,
            IAbpSession abpSession)
        {
            _historyRepository = historyRepository;
            _abpSession = abpSession;
        }

        public Task WriteAsync(int staffId, int? branchId, string action, string description)
        {
            return _historyRepository.InsertAsync(new StaffHistory
            {
                TenantId = _abpSession.TenantId,
                BranchId = branchId,
                StaffId = staffId,
                Action = action,
                Description = description
            });
        }
    }
}
