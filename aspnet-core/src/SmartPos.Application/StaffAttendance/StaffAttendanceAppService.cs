using System;
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

            return Repository.GetAll()
                .Include(x => x.Staff)
                .Include(x => x.Branch)
                .WhereIf(branchId.HasValue, x => x.BranchId == branchId.Value)
                .WhereIf(input.StaffId.HasValue, x => x.StaffId == input.StaffId.Value)
                .WhereIf(input.Status.HasValue, x => x.Status == input.Status.Value)
                .WhereIf(input.FromDate.HasValue, x => x.AttendanceDate >= input.FromDate.Value.Date)
                .WhereIf(input.ToDate.HasValue, x => x.AttendanceDate <= input.ToDate.Value.Date)
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => (x.Staff != null && x.Staff.Name.Contains(input.Keyword))
                         || (x.Remarks != null && x.Remarks.Contains(input.Keyword)));
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
