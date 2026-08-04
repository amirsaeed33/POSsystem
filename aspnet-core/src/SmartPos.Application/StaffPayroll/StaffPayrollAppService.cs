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
using SmartPos.StaffPayrolls.Dto;
using SmartPos.Staffs;

namespace SmartPos.StaffPayrolls
{
    [AbpAuthorize(PermissionNames.Pages_StaffPayroll)]
    public class StaffPayrollAppService :
        AsyncCrudAppService<StaffPayroll, StaffPayrollDto, int, PagedStaffPayrollResultRequestDto, CreateStaffPayrollDto, StaffPayrollDto>,
        IStaffPayrollAppService
    {
        private readonly IRepository<Staff> _staffRepository;
        private readonly IRepository<User, long> _userRepository;
        private readonly IBranchAccessChecker _branchAccessChecker;
        private readonly IBranchContext _branchContext;
        private readonly StaffHistoryWriter _staffHistoryWriter;

        public StaffPayrollAppService(
            IRepository<StaffPayroll> repository,
            IRepository<Staff> staffRepository,
            IRepository<User, long> userRepository,
            IBranchAccessChecker branchAccessChecker,
            IBranchContext branchContext,
            StaffHistoryWriter staffHistoryWriter)
            : base(repository)
        {
            _staffRepository = staffRepository;
            _userRepository = userRepository;
            _branchAccessChecker = branchAccessChecker;
            _branchContext = branchContext;
            _staffHistoryWriter = staffHistoryWriter;

            CreatePermissionName = PermissionNames.Pages_StaffPayroll_Create;
            UpdatePermissionName = PermissionNames.Pages_StaffPayroll_Edit;
            DeletePermissionName = PermissionNames.Pages_StaffPayroll_Delete;
            GetPermissionName = PermissionNames.Pages_StaffPayroll;
            GetAllPermissionName = PermissionNames.Pages_StaffPayroll;
        }

        protected override void CheckCreatePermission()
        {
            EnsureGranted(PermissionNames.Pages_StaffPayroll_Create, PermissionNames.Pages_StaffPayroll);
        }

        protected override void CheckUpdatePermission()
        {
            EnsureGranted(PermissionNames.Pages_StaffPayroll_Edit, PermissionNames.Pages_StaffPayroll);
        }

        protected override void CheckDeletePermission()
        {
            EnsureGranted(PermissionNames.Pages_StaffPayroll_Delete, PermissionNames.Pages_StaffPayroll);
        }

        public override async Task<StaffPayrollDto> CreateAsync(CreateStaffPayrollDto input)
        {
            CheckCreatePermission();

            ValidateMoney(input.BasicSalary ?? 0, input.Allowance, input.Bonus, input.Deduction, input.OvertimeAmount);
            ValidatePeriod(input.Month, input.Year);

            var branchId = await _branchAccessChecker.RequireEffectiveBranchIdAsync();
            var staff = await GetActiveStaffAsync(input.StaffId);
            await EnsureUniquePayrollAsync(staff.Id, input.Year, input.Month, branchId, null);

            var basicSalary = input.BasicSalary ?? staff.BasicSalary ?? 0;
            ValidateMoney(basicSalary, input.Allowance, input.Bonus, input.Deduction, input.OvertimeAmount);

            var entity = new StaffPayroll
            {
                TenantId = AbpSession.TenantId,
                BranchId = branchId,
                StaffId = staff.Id,
                Month = input.Month,
                Year = input.Year,
                BasicSalary = basicSalary,
                Allowance = input.Allowance,
                Bonus = input.Bonus,
                Deduction = input.Deduction,
                OvertimeAmount = input.OvertimeAmount,
                NetSalary = CalculateNetSalary(basicSalary, input.Allowance, input.Bonus, input.OvertimeAmount, input.Deduction),
                PaymentStatus = input.PaymentStatus,
                PaymentDate = input.PaymentDate,
                Remarks = input.Remarks
            };

            await Repository.InsertAsync(entity);
            await CurrentUnitOfWork.SaveChangesAsync();

            await _staffHistoryWriter.WriteAsync(
                staff.Id,
                branchId,
                StaffHistoryAction.SalaryChanged,
                $"Payroll created for {input.Month}/{input.Year}. NetSalary={entity.NetSalary}");

            return await GetAsync(new EntityDto<int>(entity.Id));
        }

        public override async Task<StaffPayrollDto> UpdateAsync(StaffPayrollDto input)
        {
            CheckUpdatePermission();

            ValidateMoney(input.BasicSalary, input.Allowance, input.Bonus, input.Deduction, input.OvertimeAmount);
            ValidatePeriod(input.Month, input.Year);

            var entity = await GetEntityByIdAsync(input.Id);
            await _branchAccessChecker.EnsureCanAccessBranchAsync(entity.BranchId);

            var staff = await GetActiveStaffAsync(input.StaffId);
            await EnsureUniquePayrollAsync(staff.Id, input.Year, input.Month, entity.BranchId, entity.Id);

            var previousBasic = entity.BasicSalary;
            var previousNet = entity.NetSalary;

            entity.StaffId = staff.Id;
            entity.Month = input.Month;
            entity.Year = input.Year;
            entity.BasicSalary = input.BasicSalary;
            entity.Allowance = input.Allowance;
            entity.Bonus = input.Bonus;
            entity.Deduction = input.Deduction;
            entity.OvertimeAmount = input.OvertimeAmount;
            entity.NetSalary = CalculateNetSalary(
                input.BasicSalary, input.Allowance, input.Bonus, input.OvertimeAmount, input.Deduction);
            entity.PaymentStatus = input.PaymentStatus;
            entity.PaymentDate = input.PaymentDate;
            entity.Remarks = input.Remarks;

            await Repository.UpdateAsync(entity);
            await CurrentUnitOfWork.SaveChangesAsync();

            if (previousBasic != entity.BasicSalary || previousNet != entity.NetSalary)
            {
                await _staffHistoryWriter.WriteAsync(
                    staff.Id,
                    entity.BranchId,
                    StaffHistoryAction.SalaryChanged,
                    $"Payroll updated for {entity.Month}/{entity.Year}. BasicSalary {previousBasic} → {entity.BasicSalary}, NetSalary {previousNet} → {entity.NetSalary}");
            }

            return await GetAsync(new EntityDto<int>(entity.Id));
        }

        public override async Task DeleteAsync(EntityDto<int> input)
        {
            CheckDeletePermission();

            var entity = await Repository.GetAsync(input.Id);
            await _branchAccessChecker.EnsureCanAccessBranchAsync(entity.BranchId);
            await Repository.DeleteAsync(entity);
        }

        protected override IQueryable<StaffPayroll> CreateFilteredQuery(PagedStaffPayrollResultRequestDto input)
        {
            var branchId = BranchQueryHelper.ResolveBranchIdForFilter(
                _branchContext, _userRepository, AbpSession, PermissionChecker);

            return Repository.GetAll()
                .Include(x => x.Staff)
                .Include(x => x.Branch)
                .WhereIf(branchId.HasValue, x => x.BranchId == branchId.Value)
                .WhereIf(input.StaffId.HasValue, x => x.StaffId == input.StaffId.Value)
                .WhereIf(input.Month.HasValue, x => x.Month == input.Month.Value)
                .WhereIf(input.Year.HasValue, x => x.Year == input.Year.Value)
                .WhereIf(input.PaymentStatus.HasValue, x => x.PaymentStatus == input.PaymentStatus.Value)
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => (x.Staff != null && x.Staff.Name.Contains(input.Keyword))
                         || (x.Remarks != null && x.Remarks.Contains(input.Keyword)));
        }

        protected override async Task<StaffPayroll> GetEntityByIdAsync(int id)
        {
            var entity = await Repository.GetAll()
                .Include(x => x.Staff)
                .Include(x => x.Branch)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
            {
                throw new UserFriendlyException("Payroll record not found");
            }

            return entity;
        }

        protected override StaffPayrollDto MapToEntityDto(StaffPayroll entity)
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

        private async Task EnsureUniquePayrollAsync(int staffId, int year, int month, int branchId, int? excludeId)
        {
            var exists = await Repository.GetAll()
                .AnyAsync(x => x.StaffId == staffId
                               && x.Year == year
                               && x.Month == month
                               && x.BranchId == branchId
                               && (!excludeId.HasValue || x.Id != excludeId.Value));

            if (exists)
            {
                throw new UserFriendlyException("Payroll already exists for this staff for the selected month and year.");
            }
        }

        private static void ValidatePeriod(int month, int year)
        {
            if (month < 1 || month > 12)
            {
                throw new UserFriendlyException("Month must be between 1 and 12.");
            }

            if (year < 2000 || year > 2100)
            {
                throw new UserFriendlyException("Year is out of allowed range.");
            }
        }

        private static void ValidateMoney(decimal basicSalary, decimal allowance, decimal bonus, decimal deduction, decimal overtimeAmount)
        {
            if (basicSalary < 0 || allowance < 0 || bonus < 0 || deduction < 0 || overtimeAmount < 0)
            {
                throw new UserFriendlyException("Salary values cannot be negative.");
            }
        }

        private static decimal CalculateNetSalary(
            decimal basicSalary,
            decimal allowance,
            decimal bonus,
            decimal overtimeAmount,
            decimal deduction)
        {
            return basicSalary + allowance + bonus + overtimeAmount - deduction;
        }
    }
}