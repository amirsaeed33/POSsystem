using System;
using System.ComponentModel.DataAnnotations;
using Abp.AutoMapper;
using SmartPos.Staffs;

namespace SmartPos.StaffPayrolls.Dto
{
    [AutoMapTo(typeof(StaffPayroll))]
    public class CreateStaffPayrollDto
    {
        [Required]
        public int StaffId { get; set; }

        [Range(1, 12)]
        public int Month { get; set; }

        [Range(2000, 2100)]
        public int Year { get; set; }

        public decimal? BasicSalary { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Allowance { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Bonus { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Deduction { get; set; }

        [Range(0, double.MaxValue)]
        public decimal OvertimeAmount { get; set; }

        public PayrollPaymentStatus PaymentStatus { get; set; }

        public DateTime? PaymentDate { get; set; }

        [StringLength(StaffPayroll.MaxRemarksLength)]
        public string Remarks { get; set; }
    }
}
