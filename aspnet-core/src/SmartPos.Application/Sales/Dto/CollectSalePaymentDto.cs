using System.ComponentModel.DataAnnotations;

namespace SmartPos.Sales.Dto
{
    public class CollectSalePaymentDto
    {
        [Required]
        public int SaleId { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Payment amount must be greater than zero.")]
        public decimal Amount { get; set; }

        /// <summary><see cref="PaymentTypes"/>: 1 = Cash, 2 = Card</summary>
        public int PaymentMethod { get; set; } = PaymentTypes.Cash;

        [StringLength(500)]
        public string Notes { get; set; }
    }
}

