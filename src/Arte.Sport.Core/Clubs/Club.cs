using Arte.Sport.Core.Branching;

namespace Arte.Sport.Core.Clubs;

public sealed class Brand
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class Branch
{
    public Guid Id { get; set; }
    public Guid BrandId { get; set; }
    public string Name { get; set; } = "";
    public string TimeZone { get; set; } = "Asia/Tehran";
    public bool IsActive { get; set; } = true;
    public bool MemberCanCancel { get; set; } = true;
    /// <summary>Minutes before start. Zero means the member can cancel until the session starts.</summary>
    public int CancelUntilMinutes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class Membership : IBranchOwned
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public Guid BranchId { get; set; }
    public string Role { get; set; } = "";
    public string[] Permissions { get; set; } = [];
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }

    public bool Has(string permission) => Permissions.Contains(permission);
}
