using System.ComponentModel.DataAnnotations;
using SmartPos.Customers;

namespace SmartPos.Customers.Dto
{
    public class GetCustomerFavoritesInput
    {
        public int? CustomerId { get; set; }

        public int? BranchId { get; set; }

        [StringLength(Customer.MaxPhoneLength)]
        public string Phone { get; set; }
    }

    public class ToggleCustomerFavoriteInput
    {
        public int ProductId { get; set; }

        public int? CustomerId { get; set; }

        public int? BranchId { get; set; }

        [StringLength(Customer.MaxPhoneLength)]
        public string Phone { get; set; }
    }

    public class ToggleCustomerFavoriteResultDto
    {
        public int ProductId { get; set; }

        public bool IsFavorite { get; set; }
    }
}
