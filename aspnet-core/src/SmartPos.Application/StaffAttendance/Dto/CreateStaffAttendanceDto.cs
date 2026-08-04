using System;
using System.ComponentModel.DataAnnotations;
using Abp.AutoMapper;
using SmartPos.Staffs;

namespace SmartPos.StaffAttendances.Dto
{
    [AutoMapTo(typeof(StaffAttendance))]
    public class CreateStaffAttendanceDto
    {
        [Required]
        public int StaffId { get; set; }

        [Required]
        public DateTime AttendanceDate { get; set; }

        public DateTime? CheckInTime { get; set; }

        public DateTime? CheckOutTime { get; set; }

        public AttendanceStatus Status { get; set; }

        [StringLength(StaffAttendance.MaxRemarksLength)]
        public string Remarks { get; set; }
    }
}
