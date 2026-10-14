using System;
using System.ComponentModel.DataAnnotations;
using SmartPos.Staffs;

namespace SmartPos.StaffPayrolls.Dto
{
    public class ProcessStaffPayrollPaymentDto
    {
        [Required]
        public int PayrollId { get; set; }

        public int? PaymentAccountId { get; set; }

        public DateTime? PaymentDate { get; set; }

        [StringLength(StaffPayroll.MaxRemarksLength)]
        public string Remarks { get; set; }
    }
}
