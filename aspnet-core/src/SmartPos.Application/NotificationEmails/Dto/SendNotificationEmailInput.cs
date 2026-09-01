using System.ComponentModel.DataAnnotations;

namespace SmartPos.NotificationEmails.Dto
{
    public class SendNotificationEmailInput
    {
        public int? BranchId { get; set; }

        [EmailAddress]
        [StringLength(256)]
        public string TargetEmail { get; set; }
    }
}
