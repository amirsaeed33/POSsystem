using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartPos.Suppliers.Dto
{
    public class SetSupplierProductsDto
    {
        [Range(1, int.MaxValue)]
        public int SupplierId { get; set; }

        public List<int> ProductIds { get; set; } = new List<int>();
    }
}
