using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using SmartPos.Branches;

namespace SmartPos.Staffs
{
    [Table("AppStaff")]
    public class Staff : FullAuditedEntity, IMayHaveTenant
    {
        public const int MaxNameLength = 128;
        public const int MaxPhoneLength = 32;
        public const int MaxEmailLength = 256;
        public const int MaxAddressLength = 512;
        public const int MaxDesignationLength = 128;
        public const int MaxEmployeeCodeLength = 64;

        public virtual int? TenantId { get; set; }

        public virtual int? BranchId { get; set; }

        [ForeignKey(nameof(BranchId))]
        public virtual Branch Branch { get; set; }

        [StringLength(MaxEmployeeCodeLength)]
        public virtual string EmployeeCode { get; set; }

        [Required]
        [StringLength(MaxNameLength)]
        public virtual string Name { get; set; }

        [StringLength(MaxPhoneLength)]
        public virtual string Phone { get; set; }

        [StringLength(MaxEmailLength)]
        public virtual string Email { get; set; }

        [StringLength(MaxAddressLength)]
        public virtual string Address { get; set; }

        [StringLength(MaxDesignationLength)]
        public virtual string Designation { get; set; }

        public virtual DateTime JoiningDate { get; set; }

        public virtual decimal? BasicSalary { get; set; }

        public virtual bool IsActive { get; set; } = true;

        /// <summary>
        /// Linked AbpUsers login account (optional).
        /// </summary>
        public virtual long? UserId { get; set; }

        public virtual ICollection<StaffAttendance> Attendances { get; set; }

        public virtual ICollection<StaffPayroll> Payrolls { get; set; }
    }
}
