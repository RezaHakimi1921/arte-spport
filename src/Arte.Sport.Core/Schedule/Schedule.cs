using Arte.Sport.Core.Branching;

namespace Arte.Sport.Core.Schedule;

public sealed class GymSession : IBranchOwned
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string Title { get; set; } = "";
    public string Kind { get; set; } = "private";
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public string? PlaceName { get; set; }
    public Guid CoachPersonId { get; set; }
    public string Status { get; set; } = "published";
    public int? Capacity { get; set; }
}

public sealed class SessionBooking : IBranchOwned
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public Guid SessionId { get; set; }
    public Guid MemberProfileId { get; set; }
    public string Status { get; set; } = "booked";
    public DateTimeOffset? CancelledAt { get; set; }
}

public sealed class MemberAttendance : IBranchOwned
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public Guid SessionId { get; set; }
    public Guid MemberProfileId { get; set; }
    public string Status { get; set; } = "present";
    public string? CorrectionReason { get; set; }
    public Guid RecordedBy { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
}

public sealed class StaffShift : IBranchOwned
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public Guid PersonId { get; set; }
    public int Weekday { get; set; }
    public TimeOnly StartsAt { get; set; }
    public TimeOnly EndsAt { get; set; }
}

public sealed class StaffPunch : IBranchOwned
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public Guid PersonId { get; set; }
    public DateTimeOffset InAt { get; set; }
    public DateTimeOffset? OutAt { get; set; }
    public string? CloseReason { get; set; }
}

public sealed class InboxNote : IBranchOwned
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public Guid RecipientPersonId { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public bool Seen { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
