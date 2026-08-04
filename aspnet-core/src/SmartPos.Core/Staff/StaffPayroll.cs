using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using SmartPos.Branches;

namespace SmartPos.Staffs
{
    [Table("AppStaffPayrolls")]
    public class StaffPayroll : FullAuditedEntity, IMayHaveTenant
    {
        public const int MaxRemarksLength = 512;

        public virtual int? TenantId { get; set; }

        public virtual int BranchId { get; set; }

        [ForeignKey(nameof(BranchId))]
        public virtual Branch Branch { get; set; }

        public virtual int StaffId { get; set; }

        [ForeignKey(nameof(StaffId))]
        public virtual Staff Staff { get; set; }

        public virtual int Month { get; set; }

        public virtual int Year { get; set; }

        public virtual decimal BasicSalary { get; set; }

        public virtual decimal Allowance { get; set; }

        public virtual decimal Bonus { get; set; }

        public virtual decimal Deduction { get; set; }

        public virtual decimal OvertimeAmount { get; set; }

        public virtual decimal NetSalary { get; set; }

        public virtual PayrollPaymentStatus PaymentStatus { get; set; }

        public virtual DateTime? PaymentDate { get; set; }

        [StringLength(MaxRemarksLength)]
        public virtual string Remarks { get; set; }
    }
}
