using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using SmartPos.Authorization;
using SmartPos.Authorization.Users;
using SmartPos.Branches;
using SmartPos.StaffAttendances.Dto;
using SmartPos.Staffs;

namespace SmartPos.StaffAttendances
{
    [AbpAuthorize(PermissionNames.Pages_StaffAttendance)]
    public class StaffAttendanceAppService :
        AsyncCrudAppService<StaffAttendance, StaffAttendanceDto, int, PagedStaffAttendanceResultRequestDto, CreateStaffAttendanceDto, StaffAttendanceDto>,
        IStaffAttendanceAppService
    {
        private readonly IRepository<Staff> _staffRepository;
        private readonly IRepository<User, long> _userRepository;
        private readonly IBranchAccessChecker _branchAccessChecker;
        private readonly IBranchContext _branchContext;

        public StaffAttendanceAppService(
            IRepository<StaffAttendance> repository,
            IRepository<Staff> staffRepository,
            IRepository<User, long> userRepository,
            IBranchAccessChecker branchAccessChecker,
            IBranchContext branchContext)
            : base(repository)
        {
            _staffRepository = staffRepository;
            _userRepository = userRepository;
            _branchAccessChecker = branchAccessChecker;
            _branchContext = branchContext;

            CreatePermissionName = PermissionNames.Pages_StaffAttendance_Create;
            UpdatePermissionName = PermissionNames.Pages_StaffAttendance_Edit;
            DeletePermissionName = PermissionNames.Pages_StaffAttendance_Delete;
            GetPermissionName = PermissionNames.Pages_StaffAttendance;
            GetAllPermissionName = PermissionNames.Pages_StaffAttendance;
        }

        protected override void CheckCreatePermission()
        {
            EnsureGranted(PermissionNames.Pages_StaffAttendance_Create, PermissionNames.Pages_StaffAttendance);
        }

        protected override void CheckUpdatePermission()
        {
            EnsureGranted(PermissionNames.Pages_StaffAttendance_Edit, PermissionNames.Pages_StaffAttendance);
        }

        protected override void CheckDeletePermission()
        {
            EnsureGranted(PermissionNames.Pages_StaffAttendance_Delete, PermissionNames.Pages_StaffAttendance);
        }

        /// <summary>
        /// Active staff for the current branch with that day's attendance (if any).
        /// </summary>
        public async Task<ListResultDto<DailyAttendanceSheetItemDto>> GetDailySheetAsync(
            GetDailyAttendanceSheetInput input)
        {
            CheckGetAllPermission();

            var day = ParseDateOnly(input?.Date);
            var branchId = BranchQueryHelper.ResolveBranchIdForFilter(
                _branchContext, _userRepository, AbpSession, PermissionChecker);

            var staffQuery = _staffRepository.GetAll()
                .AsNoTracking()
                .Include(x => x.Branch)
                .Where(x => x.IsActive)
                .WhereIf(branchId.HasValue, x => x.BranchId == branchId.Value)
                .WhereIf(input.StaffId.HasValue, x => x.Id == input.StaffId.Value)
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => x.Name.Contains(input.Keyword)
                         || (x.Designation != null && x.Designation.Contains(input.Keyword))
                         || (x.EmployeeCode != null && x.EmployeeCode.Contains(input.Keyword)));

            var staffList = await staffQuery
                .OrderBy(x => x.Name)
                .ToListAsync();

            var staffIds = staffList.Select(x => x.Id).ToList();
            var dayEnd = day.AddDays(1);

            var attendanceList = staffIds.Count == 0
                ? new List<StaffAttendance>()
                : await Repository.GetAll()
                    .AsNoTracking()
                    .Where(x => staffIds.Contains(x.StaffId)
                                && x.AttendanceDate >= day
                                && x.AttendanceDate < dayEnd)
                    .ToListAsync();

            var attendanceByStaff = attendanceList
                .GroupBy(x => x.StaffId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.Id).First());

            var items = staffList.Select(staff =>
            {
                attendanceByStaff.TryGetValue(staff.Id, out var att);
                return new DailyAttendanceSheetItemDto
                {
                    Id = att?.Id ?? 0,
                    StaffId = staff.Id,
                    StaffName = staff.Name,
                    Designation = staff.Designation,
                    BranchName = staff.Branch?.Name,
                    BranchId = att?.BranchId ?? staff.BranchId,
                    AttendanceDate = day,
                    HasAttendance = att != null,
                    AttendanceId = att?.Id,
                    CheckInTime = att?.CheckInTime,
                    CheckOutTime = att?.CheckOutTime,
                    Status = att?.Status,
                    WorkingHours = att?.WorkingHours,
                    Remarks = att?.Remarks
                };
            }).ToList();

            return new ListResultDto<DailyAttendanceSheetItemDto>(items);
        }

        public override async Task<StaffAttendanceDto> CreateAsync(CreateStaffAttendanceDto input)
        {
            CheckCreatePermission();

            var branchId = await _branchAccessChecker.RequireEffectiveBranchIdAsync();
            var staff = await GetActiveStaffAsync(input.StaffId);
            await EnsureUniqueAttendanceAsync(input.StaffId, input.AttendanceDate.Date, null);

            var entity = new StaffAttendance
            {
                TenantId = AbpSession.TenantId,
                BranchId = branchId,
                StaffId = staff.Id,
                AttendanceDate = input.AttendanceDate.Date,
                Status = input.Status,
                Remarks = input.Remarks
            };

            ApplyAttendanceTimes(entity, input.Status, input.CheckInTime, input.CheckOutTime);

            await Repository.InsertAsync(entity);
            await CurrentUnitOfWork.SaveChangesAsync();

            return await GetAsync(new EntityDto<int>(entity.Id));
        }

        public override async Task<StaffAttendanceDto> UpdateAsync(StaffAttendanceDto input)
        {
            CheckUpdatePermission();

            var entity = await GetEntityByIdAsync(input.Id);
            await _branchAccessChecker.EnsureCanAccessBranchAsync(entity.BranchId);

            var staff = await GetActiveStaffAsync(input.StaffId);
            await EnsureUniqueAttendanceAsync(input.StaffId, input.AttendanceDate.Date, entity.Id);

            entity.StaffId = staff.Id;
            entity.AttendanceDate = input.AttendanceDate.Date;
            entity.Status = input.Status;
            entity.Remarks = input.Remarks;
            ApplyAttendanceTimes(entity, input.Status, input.CheckInTime, input.CheckOutTime);

            await Repository.UpdateAsync(entity);
            await CurrentUnitOfWork.SaveChangesAsync();

            return await GetAsync(new EntityDto<int>(entity.Id));
        }

        public override async Task DeleteAsync(EntityDto<int> input)
        {
            CheckDeletePermission();

            var entity = await Repository.GetAsync(input.Id);
            await _branchAccessChecker.EnsureCanAccessBranchAsync(entity.BranchId);
            await Repository.DeleteAsync(entity);
        }

        protected override IQueryable<StaffAttendance> CreateFilteredQuery(PagedStaffAttendanceResultRequestDto input)
        {
            var branchId = BranchQueryHelper.ResolveBranchIdForFilter(
                _branchContext, _userRepository, AbpSession, PermissionChecker);

            DateTime? fromDate = input.FromDate.HasValue
                ? ToLocalDate(input.FromDate.Value)
                : (DateTime?)null;
            DateTime? toDateExclusive = input.ToDate.HasValue
                ? ToLocalDate(input.ToDate.Value).AddDays(1)
                : (DateTime?)null;

            return Repository.GetAll()
                .Include(x => x.Staff)
                .Include(x => x.Branch)
                .WhereIf(
                    branchId.HasValue,
                    x => x.BranchId == branchId.Value
                         || (x.Staff != null && x.Staff.BranchId == branchId.Value))
                .WhereIf(input.StaffId.HasValue, x => x.StaffId == input.StaffId.Value)
                .WhereIf(input.Status.HasValue, x => x.Status == input.Status.Value)
                .WhereIf(fromDate.HasValue, x => x.AttendanceDate >= fromDate.Value)
                .WhereIf(toDateExclusive.HasValue, x => x.AttendanceDate < toDateExclusive.Value)
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => (x.Staff != null && x.Staff.Name.Contains(input.Keyword))
                         || (x.Remarks != null && x.Remarks.Contains(input.Keyword)));
        }

        private static DateTime ToLocalDate(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc)
            {
                return value.ToLocalTime().Date;
            }

            return value.Date;
        }

        private static DateTime ParseDateOnly(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return DateTime.Today;
            }

            var raw = value.Trim();
            if (raw.Length >= 10
                && DateTime.TryParseExact(
                    raw.Substring(0, 10),
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var dateOnly))
            {
                return dateOnly.Date;
            }

            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
            {
                return ToLocalDate(parsed);
            }

            throw new UserFriendlyException("Invalid attendance date.");
        }

        protected override async Task<StaffAttendance> GetEntityByIdAsync(int id)
        {
            var entity = await Repository.GetAll()
                .Include(x => x.Staff)
                .Include(x => x.Branch)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
            {
                throw new UserFriendlyException("Attendance record not found");
            }

            return entity;
        }

        protected override StaffAttendanceDto MapToEntityDto(StaffAttendance entity)
        {
            var dto = base.MapToEntityDto(entity);
            dto.StaffName = entity.Staff?.Name;
            dto.BranchName = entity.Branch?.Name;
            return dto;
        }

        private void EnsureGranted(string childPermission, string parentPermission)
        {
            if (IsGranted(childPermission) || IsGranted(parentPermission))
            {
                return;
            }

            throw new AbpAuthorizationException(
                "You are not authorized to perform this action.");
        }

        private async Task<Staff> GetActiveStaffAsync(int staffId)
        {
            var staff = await _staffRepository.FirstOrDefaultAsync(staffId);
            if (staff == null)
            {
                throw new UserFriendlyException("Staff not found");
            }

            if (!staff.IsActive)
            {
                throw new UserFriendlyException("Staff is inactive");
            }

            return staff;
        }

        private async Task EnsureUniqueAttendanceAsync(int staffId, DateTime attendanceDate, int? excludeId)
        {
            var exists = await Repository.GetAll()
                .AnyAsync(x => x.StaffId == staffId
                               && x.AttendanceDate == attendanceDate
                               && (!excludeId.HasValue || x.Id != excludeId.Value));

            if (exists)
            {
                throw new UserFriendlyException("Attendance already exists for this staff on the selected date.");
            }
        }

        private static void ApplyAttendanceTimes(
            StaffAttendance entity,
            AttendanceStatus status,
            DateTime? checkInTime,
            DateTime? checkOutTime)
        {
            if (status == AttendanceStatus.Absent || status == AttendanceStatus.Leave)
            {
                entity.CheckInTime = null;
                entity.CheckOutTime = null;
                entity.WorkingHours = 0;
                return;
            }

            if (checkInTime.HasValue && checkOutTime.HasValue && checkOutTime < checkInTime)
            {
                throw new UserFriendlyException("Check-out time cannot be earlier than check-in time.");
            }

            if (status == AttendanceStatus.Present && !checkInTime.HasValue)
            {
                throw new UserFriendlyException("Check-in time is required for Present status.");
            }

            entity.CheckInTime = checkInTime;
            entity.CheckOutTime = checkOutTime;

            if (checkInTime.HasValue && checkOutTime.HasValue)
            {
                entity.WorkingHours = Math.Round((decimal)(checkOutTime.Value - checkInTime.Value).TotalHours, 2);
            }
            else
            {
                entity.WorkingHours = null;
            }
        }
    }
}
