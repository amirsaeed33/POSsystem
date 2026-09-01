using System;
using System.Linq;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Threading.BackgroundWorkers;
using Abp.Threading.Timers;
using SmartPos.Authorization.Users;
using SmartPos.MultiTenancy;
using SmartPos.NotificationEmails.Dto;

namespace SmartPos.NotificationEmails
{
    public class AutomatedEmailBackgroundWorker : PeriodicBackgroundWorkerBase, ISingletonDependency
    {
        private readonly IRepository<Tenant> _tenantRepository;
        private readonly IRepository<User, long> _userRepository;
        private readonly INotificationEmailAppService _notificationEmailAppService;

        public AutomatedEmailBackgroundWorker(
            AbpTimer timer,
            IRepository<Tenant> tenantRepository,
            IRepository<User, long> userRepository,
            INotificationEmailAppService notificationEmailAppService)
            : base(timer)
        {
            _tenantRepository = tenantRepository;
            _userRepository = userRepository;
            _notificationEmailAppService = notificationEmailAppService;

            // Timer set to run every 12 hours (43,200,000 milliseconds)
            Timer.Period = 43200000;
        }

        [UnitOfWork]
        protected override void DoWork()
        {
            try
            {
                var activeTenants = _tenantRepository.GetAllList(t => t.IsActive);
                foreach (var tenant in activeTenants)
                {
                    using (CurrentUnitOfWork.SetTenantId(tenant.Id))
                    {
                        var adminUser = _userRepository.FirstOrDefault(u => u.IsActive && u.EmailAddress != null && u.EmailAddress != "");
                        if (adminUser != null && !string.IsNullOrWhiteSpace(adminUser.EmailAddress))
                        {
                            try
                            {
                                _notificationEmailAppService.SendLowStockReportAsync(new SendNotificationEmailInput
                                {
                                    TargetEmail = adminUser.EmailAddress
                                }).Wait();

                                _notificationEmailAppService.SendDailyBusinessSummaryAsync(new SendNotificationEmailInput
                                {
                                    TargetEmail = adminUser.EmailAddress
                                }).Wait();
                            }
                            catch
                            {
                                // Log or handle silent retry for worker
                            }
                        }
                    }
                }
            }
            catch
            {
                // Background worker error suppression
            }
        }
    }
}
