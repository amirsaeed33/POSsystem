using System;
using System.ComponentModel.DataAnnotations;
using Abp.AutoMapper;
using SmartPos.Staffs;

namespace SmartPos.Staffs.Dto
{
    [AutoMapTo(typeof(Staff))]
    public class CreateStaffDto
    {
        public int? BranchId { get; set; }

        [StringLength(Staff.MaxEmployeeCodeLength)]
        public string EmployeeCode { get; set; }

        [Required]
        [StringLength(Staff.MaxNameLength)]
        public string Name { get; set; }

        [StringLength(Staff.MaxPhoneLength)]
        public string Phone { get; set; }

        [EmailAddress]
        [StringLength(Staff.MaxEmailLength)]
        public string Email { get; set; }

        [StringLength(Staff.MaxAddressLength)]
        public string Address { get; set; }

        [StringLength(Staff.MaxDesignationLength)]
        public string Designation { get; set; }

        [Required]
        public DateTime JoiningDate { get; set; }

        public decimal? BasicSalary { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
