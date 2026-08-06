namespace SmartPos.Branches
{
    /// <summary>
    /// Stable LookUp.Name values for Type = BranchStatus (host-scoped rows).
    /// Branches store the relationship as <see cref="Branch.StatusId"/>.
    /// </summary>
    public static class BranchStatuses
    {
        public const string Pending = "Pending";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";

        public static bool IsApproved(string statusName)
        {
            return string.Equals(statusName, Approved, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
