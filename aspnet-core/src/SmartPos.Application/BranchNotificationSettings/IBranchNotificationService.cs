using System;

namespace SmartPos.BranchNotificationSettings
{
    public enum BranchNotificationType
    {
        Sale = 1,
        Purchase = 2,
        Expense = 3
    }

    public interface IBranchNotificationService
    {
        void SendNotificationAfterCommit(
            BranchNotificationType type,
            int? tenantId,
            int branchId,
            string referenceNo,
            decimal totalAmount,
            long? creatorUserId,
            DateTime dateTime);
    }
}
