namespace Arte.Sport.Core.Branching;

/// <summary>Row belongs to one branch. The database filter hides every other branch.</summary>
public interface IBranchOwned
{
    Guid BranchId { get; set; }
}

public interface ISoftDeletable
{
    DateTimeOffset? DeletedAt { get; set; }
    Guid? DeletedBy { get; set; }
}

public sealed class BranchViolationException(string message) : Exception(message);
