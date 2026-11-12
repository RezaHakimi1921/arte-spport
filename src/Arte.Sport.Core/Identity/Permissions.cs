namespace Arte.Sport.Core.Identity;

public static class SportPermissions
{
    public const string BranchSettings = "branch.settings";
    public const string StaffManage = "staff.manage";
    public const string MembersManage = "members.manage";
    public const string MembersRead = "members.read";
    public const string MembersReadAssigned = "members.read_assigned";
    public const string SessionsManage = "sessions.manage";
    public const string AttendanceRecord = "attendance.record";
    public const string FinanceApprove = "finance.approve";
    public const string PlansManage = "plans.manage";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        BranchSettings, StaffManage, MembersManage, MembersRead, MembersReadAssigned,
        SessionsManage, AttendanceRecord, FinanceApprove, PlansManage,
    };
}

public static class SportRoles
{
    public const string Manager = "manager";
    public const string Secretary = "secretary";
    public const string Coach = "coach";
    public const string Member = "member";

    public static readonly IReadOnlySet<string> All = new HashSet<string> { Manager, Secretary, Coach, Member };

    /// <summary>A role only seeds permissions. A manager can change them per person, never above their own.</summary>
    public static string[] DefaultPermissions(string role) => role switch
    {
        Manager => [.. SportPermissions.All],
        Secretary =>
        [
            SportPermissions.StaffManage, SportPermissions.MembersManage, SportPermissions.MembersRead,
            SportPermissions.SessionsManage, SportPermissions.AttendanceRecord,
        ],
        Coach =>
        [
            SportPermissions.MembersReadAssigned, SportPermissions.AttendanceRecord, SportPermissions.PlansManage,
        ],
        Member => [],
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };
}
