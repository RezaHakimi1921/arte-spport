namespace Arte.Sport.Core.Identity;

public sealed class Person
{
    public Guid Id { get; set; }
    public string? Mobile { get; set; }
    /// <summary>Optional sign-in name for staff. Students keep using the mobile.</summary>
    public string? Username { get; set; }
    public string DisplayName { get; set; } = "";
    public string? PasswordHash { get; set; }
    public bool IsPlatformAdmin { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class OtpChallenge
{
    public Guid Id { get; set; }
    public string Mobile { get; set; } = "";
    public string CodeHash { get; set; } = "";
    public DateTimeOffset ExpiresAt { get; set; }
    public int Attempts { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
