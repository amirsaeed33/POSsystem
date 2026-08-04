using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using SmartPos.Branches;

namespace SmartPos.Staffs
{
    [Table("AppStaffAttendances")]
    public class StaffAttendance : FullAuditedEntity, IMayHaveTenant
    {
        public const int MaxRemarksLength = 512;

        public virtual int? TenantId { get; set; }

        public virtual int BranchId { get; set; }

        [ForeignKey(nameof(BranchId))]
        public virtual Branch Branch { get; set; }

        public virtual int StaffId { get; set; }

        [ForeignKey(nameof(StaffId))]
        public virtual Staff Staff { get; set; }

        public virtual DateTime AttendanceDate { get; set; }

        public virtual DateTime? CheckInTime { get; set; }

        public virtual DateTime? CheckOutTime { get; set; }

        public virtual AttendanceStatus Status { get; set; }

        public virtual decimal? WorkingHours { get; set; }

        [StringLength(MaxRemarksLength)]
        public virtual string Remarks { get; set; }
    }
}
