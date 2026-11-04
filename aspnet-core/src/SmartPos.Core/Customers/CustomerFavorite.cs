using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using SmartPos.Branches;
using SmartPos.Products;

namespace SmartPos.Customers
{
    /// <summary>
    /// Online / POS customer favorite product (synced by phone + branch).
    /// </summary>
    [Table("AppCustomerFavorites")]
    public class CustomerFavorite : CreationAuditedEntity, IMayHaveTenant
    {
        public virtual int? TenantId { get; set; }

        public virtual int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public virtual Customer Customer { get; set; }

        public virtual int ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public virtual Product Product { get; set; }

        public virtual int BranchId { get; set; }

        [ForeignKey(nameof(BranchId))]
        public virtual Branch Branch { get; set; }
    }
}
