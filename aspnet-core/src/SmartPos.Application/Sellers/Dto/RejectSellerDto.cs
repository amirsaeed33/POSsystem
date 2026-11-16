using System.ComponentModel.DataAnnotations;

namespace SmartPos.Sellers.Dto
{
    public class RejectSellerDto
    {
        [Range(1, int.MaxValue)]
        public int Id { get; set; }

        [StringLength(Seller.MaxRejectionReasonLength)]
        public string RejectionReason { get; set; }
    }
}
