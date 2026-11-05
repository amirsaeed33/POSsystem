using System;
using System.Collections.Generic;

namespace SmartPos.Sales.Dto
{
    public class GetDepartmentSalesInput
    {
        public int? DepartmentId { get; set; }
        public string Keyword { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class DepartmentSalesReportDto
    {
        public List<DepartmentSalesGroupDto> Departments { get; set; } = new List<DepartmentSalesGroupDto>();
        public decimal GrandTotalAmount { get; set; }
        public decimal GrandTotalQuantity { get; set; }
        public int TotalDepartmentsCount { get; set; }
        public int TotalProductsSold { get; set; }
    }

    public class DepartmentSalesGroupDto
    {
        public int? DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public decimal TotalSalesAmount { get; set; }
        public decimal TotalQuantitySold { get; set; }
        public List<DepartmentProductSaleDto> Products { get; set; } = new List<DepartmentProductSaleDto>();
    }

    public class DepartmentProductSaleDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public decimal QuantitySold { get; set; }
        public decimal TotalAmount { get; set; }
        public int? CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int? DepartmentId { get; set; }
        public string DepartmentName { get; set; }
    }
}

