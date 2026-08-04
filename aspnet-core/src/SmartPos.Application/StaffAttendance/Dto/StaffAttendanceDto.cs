using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using SmartPos.Staffs;

namespace SmartPos.StaffAttendances.Dto
{
    [AutoMapFrom(typeof(StaffAttendance))]
    [AutoMapTo(typeof(StaffAttendance))]
    public class StaffAttendanceDto : EntityDto
    {
        public int BranchId { get; set; }

        public string BranchName { get; set; }

        [Required]
        public int StaffId { get; set; }

        public string StaffName { get; set; }

        [Required]
        public DateTime AttendanceDate { get; set; }

        public DateTime? CheckInTime { get; set; }

        public DateTime? CheckOutTime { get; set; }

        public AttendanceStatus Status { get; set; }

        public decimal? WorkingHours { get; set; }

        [StringLength(StaffAttendance.MaxRemarksLength)]
        public string Remarks { get; set; }
    }
}
