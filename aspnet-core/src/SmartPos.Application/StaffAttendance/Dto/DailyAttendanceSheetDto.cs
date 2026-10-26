using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using SmartPos.Staffs;

namespace SmartPos.StaffAttendances.Dto
{
    public class GetDailyAttendanceSheetInput
    {
        /// <summary>Attendance day as yyyy-MM-dd (preferred) or any DateTime.</summary>
        [Required]
        public string Date { get; set; }

        public int? StaffId { get; set; }

        public string Keyword { get; set; }
    }

    public class DailyAttendanceSheetItemDto : EntityDto
    {
        public int StaffId { get; set; }

        public string StaffName { get; set; }

        public string Designation { get; set; }

        public string BranchName { get; set; }

        public DateTime AttendanceDate { get; set; }

        public bool HasAttendance { get; set; }

        public int? AttendanceId { get; set; }

        public int? BranchId { get; set; }

        public DateTime? CheckInTime { get; set; }

        public DateTime? CheckOutTime { get; set; }

        public AttendanceStatus? Status { get; set; }

        public decimal? WorkingHours { get; set; }

        public string Remarks { get; set; }
    }
}
