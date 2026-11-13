using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartPos.Billing.Dto
{
    public class PaymentMethodDto
    {
        public int Id { get; set; }

        public PaymentMethodKind Kind { get; set; }

        public string KindName { get; set; }

        public string AccountTitle { get; set; }

        public string AccountNumber { get; set; }

        public string BankName { get; set; }

        public string Instructions { get; set; }

        public bool IsActive { get; set; }

        public bool HasQr { get; set; }

        public string QrBase64 { get; set; }

        public string QrContentType { get; set; }
    }

    public class SavePaymentMethodInput
    {
        public int? Id { get; set; }

        public PaymentMethodKind Kind { get; set; }

        [Required]
        [StringLength(PaymentMethod.MaxAccountTitleLength)]
        public string AccountTitle { get; set; }

        [Required]
        [StringLength(PaymentMethod.MaxAccountNumberLength)]
        public string AccountNumber { get; set; }

        [StringLength(PaymentMethod.MaxBankNameLength)]
        public string BankName { get; set; }

        [StringLength(PaymentMethod.MaxInstructionsLength)]
        public string Instructions { get; set; }

        public bool IsActive { get; set; } = true;

        public string QrBase64 { get; set; }

        public string QrContentType { get; set; }

        public bool ClearQr { get; set; }
    }

    public class AssignablePlanDto
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public decimal MonthlyAmount { get; set; }

        public decimal YearlyAmount { get; set; }
    }

    public class TenantSubscriptionDto
    {
        public int? Id { get; set; }

        public int TenantId { get; set; }

        public string TenancyName { get; set; }

        public string TenantName { get; set; }

        public int? PackageId { get; set; }

        public string PlanName { get; set; }

        public BillingCycle Cycle { get; set; }

        public string CycleName { get; set; }

        public decimal Amount { get; set; }

        public DateTime? PaidUntil { get; set; }

        public int DaysLeft { get; set; }

        public string Status { get; set; }

        public bool CanPay { get; set; }

        public bool HasPendingPayment { get; set; }
    }

    public class SaveTenantSubscriptionInput
    {
        public int TenantId { get; set; }

        public int? PackageId { get; set; }

        public BillingCycle Cycle { get; set; }

        public decimal Amount { get; set; }

        public DateTime? PaidUntil { get; set; }

        public bool ClearPaidUntil { get; set; }
    }

    public class SubscriptionPaymentDto
    {
        public int Id { get; set; }

        public int TenantId { get; set; }

        public string TenancyName { get; set; }

        public string TenantName { get; set; }

        public string PlanName { get; set; }

        public BillingCycle Cycle { get; set; }

        public string CycleName { get; set; }

        public decimal Amount { get; set; }

        public PaymentMethodKind Method { get; set; }

        public string MethodName { get; set; }

        public SubscriptionPaymentStatus Status { get; set; }

        public string StatusName { get; set; }

        public string ReviewNote { get; set; }

        public DateTime CreationTime { get; set; }

        public DateTime? ReviewedTime { get; set; }

        public bool HasReceipt { get; set; }
    }

    public class MySubscriptionDto
    {
        public TenantSubscriptionDto Subscription { get; set; }

        public List<PaymentMethodDto> PaymentMethods { get; set; }

        public List<SubscriptionPaymentDto> Payments { get; set; }

        public List<AssignablePlanDto> AvailablePlans { get; set; }
    }

    public class SubmitSubscriptionPaymentInput
    {
        public int PackageId { get; set; }

        public BillingCycle Cycle { get; set; }

        public int PaymentMethodId { get; set; }

        [Required]
        public string ReceiptBase64 { get; set; }

        public string ReceiptContentType { get; set; }

        public string ReceiptFileName { get; set; }
    }

    public class ReviewSubscriptionPaymentInput
    {
        public int Id { get; set; }

        [StringLength(SubscriptionPayment.MaxReviewNoteLength)]
        public string Note { get; set; }
    }

    public class SubscriptionAccessDto
    {
        public bool Expired { get; set; }

        public string PlanName { get; set; }

        public decimal Amount { get; set; }

        public DateTime? PaidUntil { get; set; }

        public int DaysLeft { get; set; }

        public bool HasPendingPayment { get; set; }
    }

    public class SubscriptionApprovalPageDto
    {
        public bool AlreadyApproved { get; set; }

        public string Message { get; set; }

        public string TenantName { get; set; }

        public string PlanName { get; set; }

        public DateTime? PaidUntil { get; set; }
    }

    public class ReceiptFileDto
    {
        public string FileName { get; set; }

        public string ContentType { get; set; }

        public string Base64 { get; set; }
    }
}
