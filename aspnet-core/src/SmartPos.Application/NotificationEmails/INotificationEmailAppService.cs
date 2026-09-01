using System.Threading.Tasks;
using Abp.Application.Services;
using SmartPos.NotificationEmails.Dto;

namespace SmartPos.NotificationEmails
{
    public interface INotificationEmailAppService : IApplicationService
    {
        Task SendLowStockReportAsync(SendNotificationEmailInput input);

        Task SendDailyBusinessSummaryAsync(SendNotificationEmailInput input);
    }
}
