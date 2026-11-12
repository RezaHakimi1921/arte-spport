using Arte.Sport.Core.Branching;

namespace Arte.Sport.Core.Members;

public sealed class MemberProfile : IBranchOwned, ISoftDeletable
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string FullName { get; set; } = "";
    public string? Mobile { get; set; }
    public string MembershipStatus { get; set; } = "active";
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}

/// <summary>One open assignment is the member's coach. Ending it keeps history.</summary>
public sealed class Assignment : IBranchOwned
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public Guid MemberProfileId { get; set; }
    public Guid CoachPersonId { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
}
