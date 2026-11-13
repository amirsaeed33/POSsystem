using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartPos.StaffAttendances.Dto
{
    public class GetMonthlyAttendanceSheetInput
    {
        [Required]
        public int StaffId { get; set; }

        /// <summary>Month as yyyy-MM (preferred) or any DateTime within the month.</summary>
        [Required]
        public string Month { get; set; }
    }

    public class MonthlyAttendanceSheetDto
    {
        public int StaffId { get; set; }

        public string StaffName { get; set; }

        public string Designation { get; set; }

        public string BranchName { get; set; }

        /// <summary>Normalized month key yyyy-MM.</summary>
        public string Month { get; set; }

        public int PresentDays { get; set; }

        public int AbsentDays { get; set; }

        public int LeaveDays { get; set; }

        public int HalfDays { get; set; }

        public int NotMarkedDays { get; set; }

        public List<DailyAttendanceSheetItemDto> Days { get; set; } = new List<DailyAttendanceSheetItemDto>();
    }
}
