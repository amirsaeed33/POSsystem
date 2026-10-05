using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SmartPos.Branches;

namespace SmartPos.BranchNotificationSettings.Dto
{
    public class CreateOrUpdateBranchNotificationSettingDto
    {
        [Required]
        public int BranchId { get; set; }

        public bool IsEnabled { get; set; }

        public bool NotifyOnSale { get; set; }

        public bool NotifyOnPurchase { get; set; }

        public bool NotifyOnExpense { get; set; }

        [StringLength(BranchNotificationSetting.MaxEmailsLength)]
        public string Emails { get; set; }

        public List<string> EmailList { get; set; }
    }
}
