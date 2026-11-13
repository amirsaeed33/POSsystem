using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities.Auditing;

namespace SmartPos.Billing
{
    /// <summary>
    /// Host-published account the business pays into. QR and details are shown at checkout.
    /// </summary>
    [Table("AppPaymentMethods")]
    public class PaymentMethod : FullAuditedEntity
    {
        public const int MaxAccountTitleLength = 128;
        public const int MaxAccountNumberLength = 64;
        public const int MaxBankNameLength = 128;
        public const int MaxInstructionsLength = 1000;
        public const int MaxContentTypeLength = 64;

        public virtual PaymentMethodKind Kind { get; set; }

        [Required]
        [StringLength(MaxAccountTitleLength)]
        public virtual string AccountTitle { get; set; }

        [Required]
        [StringLength(MaxAccountNumberLength)]
        public virtual string AccountNumber { get; set; }

        [StringLength(MaxBankNameLength)]
        public virtual string BankName { get; set; }

        [StringLength(MaxInstructionsLength)]
        public virtual string Instructions { get; set; }

        public virtual bool IsActive { get; set; } = true;

        public virtual byte[] QrImage { get; set; }

        [StringLength(MaxContentTypeLength)]
        public virtual string QrContentType { get; set; }
    }
}
