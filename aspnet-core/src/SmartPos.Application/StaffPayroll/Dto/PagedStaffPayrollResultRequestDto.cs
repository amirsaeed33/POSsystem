using Abp.Application.Services.Dto;
using SmartPos.Staffs;

namespace SmartPos.StaffPayrolls.Dto
{
    public class PagedStaffPayrollResultRequestDto : PagedResultRequestDto
    {
        public string Keyword { get; set; }

        public int? StaffId { get; set; }

        public int? Month { get; set; }

        public int? Year { get; set; }

        public PayrollPaymentStatus? PaymentStatus { get; set; }
    }
}
