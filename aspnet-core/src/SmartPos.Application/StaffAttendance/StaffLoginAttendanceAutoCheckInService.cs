using System;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Microsoft.EntityFrameworkCore;
using SmartPos.Branches;
using SmartPos.Geo;
using SmartPos.Staffs;

namespace SmartPos.StaffAttendances
{
    public interface IStaffLoginAttendanceAutoCheckInService
    {
        /// <summary>
        /// For waiter/rider staff: if login GPS is within the branch radius, mark today's attendance Present with check-in.
        /// Returns true when attendance was created or check-in was applied.
        /// </summary>
        Task<bool> TryAutoCheckInAsync(
            long userId,
            int? tenantId,
            double? latitude,
            double? longitude);
    }

    public class StaffLoginAttendanceAutoCheckInService :
        IStaffLoginAttendanceAutoCheckInService,
        ITransientDependency
    {
        private readonly IRepository<Staff> _staffRepository;
        private readonly IRepository<StaffAttendance> _attendanceRepository;
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        public StaffLoginAttendanceAutoCheckInService(
            IRepository<Staff> staffRepository,
            IRepository<StaffAttendance> attendanceRepository,
            IUnitOfWorkManager unitOfWorkManager)
        {
            _staffRepository = staffRepository;
            _attendanceRepository = attendanceRepository;
            _unitOfWorkManager = unitOfWorkManager;
        }

        public async Task<bool> TryAutoCheckInAsync(
            long userId,
            int? tenantId,
            double? latitude,
            double? longitude)
        {
            if (!tenantId.HasValue || !latitude.HasValue || !longitude.HasValue)
            {
                return false;
            }

            using (_unitOfWorkManager.Current.SetTenantId(tenantId))
            {
                var staff = await _staffRepository.GetAll()
                    .Include(x => x.Branch)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.UserId == userId && x.IsActive);

                if (staff == null || !IsWaiterOrRider(staff.Designation))
                {
                    return false;
                }

                if (!staff.BranchId.HasValue || staff.Branch == null)
                {
                    return false;
                }

                var branch = staff.Branch;
                if (!branch.Latitude.HasValue || !branch.Longitude.HasValue)
                {
                    return false;
                }

                // Only auto check-in when distance from branch is NOT greater than configured radius (default 200 feet).
                var radiusFeet = branch.AttendanceAutoCheckInRadiusFeet > 0
                    ? branch.AttendanceAutoCheckInRadiusFeet
                    : Branch.DefaultAttendanceAutoCheckInRadiusFeet;

                var distanceMeters = GeoDistance.MetersBetween(
                    latitude.Value,
                    longitude.Value,
                    branch.Latitude.Value,
                    branch.Longitude.Value);
                var distanceFeet = distanceMeters / Branch.FeetToMeters;

                if (distanceFeet > radiusFeet)
                {
                    // Farther than allowed radius → login OK, no attendance mark.
                    return false;
                }

                var today = DateTime.Now.Date;
                var now = DateTime.Now;
                var remark =
                    $"Auto check-in on login ({distanceFeet:0.#} ft from branch; limit {radiusFeet} ft)";

                var existing = await _attendanceRepository.GetAll()
                    .FirstOrDefaultAsync(x =>
                        x.StaffId == staff.Id
                        && x.AttendanceDate == today);

                if (existing == null)
                {
                    await _attendanceRepository.InsertAsync(new StaffAttendance
                    {
                        TenantId = tenantId,
                        BranchId = staff.BranchId.Value,
                        StaffId = staff.Id,
                        AttendanceDate = today,
                        Status = AttendanceStatus.Present,
                        CheckInTime = now,
                        CheckOutTime = null,
                        WorkingHours = null,
                        Remarks = remark
                    });
                    await _unitOfWorkManager.Current.SaveChangesAsync();
                    return true;
                }

                if (existing.CheckInTime.HasValue)
                {
                    return false;
                }

                if (existing.Status == AttendanceStatus.Absent
                    || existing.Status == AttendanceStatus.Leave)
                {
                    existing.Status = AttendanceStatus.Present;
                }

                existing.CheckInTime = now;
                if (string.IsNullOrWhiteSpace(existing.Remarks))
                {
                    existing.Remarks = remark;
                }

                await _attendanceRepository.UpdateAsync(existing);
                await _unitOfWorkManager.Current.SaveChangesAsync();
                return true;
            }
        }

        private static bool IsWaiterOrRider(string designation)
        {
            if (string.IsNullOrWhiteSpace(designation))
            {
                return false;
            }

            var value = designation.Trim().ToLowerInvariant();
            return value.Contains("waiter")
                   || value.Contains("rider")
                   || value.Contains("delivery");
        }
    }
}
