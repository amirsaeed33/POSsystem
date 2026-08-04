using System;
using Abp.Application.Services.Dto;
using SmartPos.Staffs;

namespace SmartPos.StaffAttendances.Dto
{
    public class PagedStaffAttendanceResultRequestDto : PagedResultRequestDto
    {
        public string Keyword { get; set; }

        public int? StaffId { get; set; }

        public AttendanceStatus? Status { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }
    }
}
