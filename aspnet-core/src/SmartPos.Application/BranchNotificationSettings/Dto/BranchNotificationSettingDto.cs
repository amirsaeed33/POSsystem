using System.Collections.Generic;
using Abp.Application.Services.Dto;

namespace SmartPos.BranchNotificationSettings.Dto
{
    public class BranchNotificationSettingDto : EntityDto<int>
    {
        public int? TenantId { get; set; }
        public int BranchId { get; set; }
        public bool IsEnabled { get; set; }
        public bool NotifyOnSale { get; set; }
        public bool NotifyOnPurchase { get; set; }
        public bool NotifyOnOnlineOrder { get; set; }
        public bool NotifyOnExpense { get; set; }
        public string Emails { get; set; }
        public List<string> EmailList { get; set; } = new List<string>();
    }
}
