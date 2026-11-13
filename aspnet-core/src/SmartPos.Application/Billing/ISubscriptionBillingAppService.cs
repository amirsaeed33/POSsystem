using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using SmartPos.Billing.Dto;

namespace SmartPos.Billing
{
    public interface ISubscriptionBillingAppService : IApplicationService
    {
        Task<List<PaymentMethodDto>> GetPaymentMethods();

        Task<PaymentMethodDto> SavePaymentMethod(SavePaymentMethodInput input);

        Task DeletePaymentMethod(int id);

        Task<List<AssignablePlanDto>> GetAssignablePlans();

        Task<List<AssignablePlanDto>> GetAvailablePlans();

        Task<List<TenantSubscriptionDto>> GetSubscriptions();

        Task<TenantSubscriptionDto> GetTenantSubscription(int tenantId);

        Task<TenantSubscriptionDto> SaveTenantSubscription(SaveTenantSubscriptionInput input);

        Task<List<SubscriptionPaymentDto>> GetPendingPayments();

        Task<ReceiptFileDto> GetReceipt(int id);

        Task<TenantSubscriptionDto> ApprovePayment(ReviewSubscriptionPaymentInput input);

        Task RejectPayment(ReviewSubscriptionPaymentInput input);

        Task<MySubscriptionDto> GetMine();

        Task<SubscriptionAccessDto> GetAccessStatus();

        Task<SubscriptionPaymentDto> SubmitPayment(SubmitSubscriptionPaymentInput input);

        Task<SubscriptionApprovalPageDto> ApproveFromEmail(string token);
    }
}
