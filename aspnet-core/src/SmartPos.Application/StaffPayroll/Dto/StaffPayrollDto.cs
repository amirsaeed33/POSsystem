using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using SmartPos.Staffs;

namespace SmartPos.StaffPayrolls.Dto
{
    [AutoMapFrom(typeof(StaffPayroll))]
    [AutoMapTo(typeof(StaffPayroll))]
    public class StaffPayrollDto : EntityDto
    {
        public int BranchId { get; set; }

        public string BranchName { get; set; }

        [Required]
        public int StaffId { get; set; }

        public string StaffName { get; set; }

        [Range(1, 12)]
        public int Month { get; set; }

        [Range(2000, 2100)]
        public int Year { get; set; }

        [Range(0, double.MaxValue)]
        public decimal BasicSalary { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Allowance { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Bonus { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Deduction { get; set; }

        [Range(0, double.MaxValue)]
        public decimal OvertimeAmount { get; set; }

        public decimal NetSalary { get; set; }

        public PayrollPaymentStatus PaymentStatus { get; set; }

        public DateTime? PaymentDate { get; set; }

        [StringLength(StaffPayroll.MaxRemarksLength)]
        public string Remarks { get; set; }
    }
}
