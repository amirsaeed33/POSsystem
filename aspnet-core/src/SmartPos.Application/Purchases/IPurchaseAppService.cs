using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using SmartPos.Purchases.Dto;

namespace SmartPos.Purchases
{
    public class MakePaymentInputDto
    {
        [Required]
        public int PurchaseId { get; set; }

        [Required]
        public int PaymentAccountId { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [StringLength(256)]
        public string Description { get; set; }
    }

    public interface IPurchaseAppService : IAsyncCrudAppService<PurchaseDto, int, PagedPurchaseResultRequestDto, CreatePurchaseDto, PurchaseDto>
    {
        Task PayPurchaseAsync(MakePaymentInputDto input);
    }
}
