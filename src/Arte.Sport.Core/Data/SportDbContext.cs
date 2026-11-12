using System.Linq.Expressions;
using Arte.Sport.Core.Branching;
using Arte.Sport.Core.Clubs;
using Arte.Sport.Core.Identity;
using Arte.Sport.Core.Members;
using Arte.Sport.Core.Schedule;
using Microsoft.EntityFrameworkCore;

namespace Arte.Sport.Core.Data;

public sealed class SportDbContext(DbContextOptions<SportDbContext> options) : DbContext(options)
{
    public const string BranchFilter = "branch";
    public const string SoftDeleteFilter = "soft_delete";

    public Guid? CurrentBranchId { get; set; }

    public DbSet<Person> People => Set<Person>();
    public DbSet<OtpChallenge> OtpChallenges => Set<OtpChallenge>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<MemberProfile> Members => Set<MemberProfile>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<GymSession> Sessions => Set<GymSession>();
    public DbSet<SessionBooking> Bookings => Set<SessionBooking>();
    public DbSet<MemberAttendance> Attendances => Set<MemberAttendance>();
    public DbSet<StaffShift> StaffShifts => Set<StaffShift>();
    public DbSet<StaffPunch> StaffPunches => Set<StaffPunch>();
    public DbSet<InboxNote> Inbox => Set<InboxNote>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Person>(e =>
        {
            e.Property(x => x.Mobile).HasMaxLength(11);
            e.Property(x => x.Username).HasMaxLength(32);
            e.Property(x => x.DisplayName).HasMaxLength(80);
            e.Property(x => x.PasswordHash).HasMaxLength(500);
            e.HasIndex(x => x.Mobile).IsUnique().HasFilter("\"Mobile\" IS NOT NULL");
            e.HasIndex(x => x.Username).IsUnique().HasFilter("\"Username\" IS NOT NULL");
        });

        b.Entity<OtpChallenge>(e =>
        {
            e.Property(x => x.Mobile).HasMaxLength(11);
            e.Property(x => x.CodeHash).HasMaxLength(128);
            e.HasIndex(x => x.Mobile);
        });

        b.Entity<Brand>(e => e.Property(x => x.Name).HasMaxLength(80));

        b.Entity<Branch>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(80);
            e.Property(x => x.TimeZone).HasMaxLength(64);
            e.HasOne<Brand>().WithMany().HasForeignKey(x => x.BrandId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Membership>(e =>
        {
            e.Property(x => x.Role).HasMaxLength(32);
            e.HasIndex(x => new { x.BranchId, x.PersonId }).IsUnique();
            e.HasOne<Person>().WithMany().HasForeignKey(x => x.PersonId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<MemberProfile>(e =>
        {
            e.Property(x => x.FullName).HasMaxLength(80);
            e.Property(x => x.Mobile).HasMaxLength(11);
            e.Property(x => x.Notes).HasMaxLength(500);
            e.Property(x => x.MembershipStatus).HasMaxLength(20);
            e.HasIndex(x => new { x.BranchId, x.Mobile })
                .IsUnique()
                .HasFilter("\"DeletedAt\" IS NULL AND \"Mobile\" IS NOT NULL");
        });

        b.Entity<Assignment>(e =>
        {
            e.HasIndex(x => new { x.BranchId, x.MemberProfileId });
            e.HasOne<MemberProfile>().WithMany().HasForeignKey(x => x.MemberProfileId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<GymSession>(e =>
        {
            e.ToTable("Sessions");
            e.Property(x => x.Title).HasMaxLength(80);
            e.Property(x => x.Kind).HasMaxLength(16);
            e.Property(x => x.PlaceName).HasMaxLength(80);
            e.Property(x => x.Status).HasMaxLength(16);
            e.HasIndex(x => new { x.BranchId, x.StartsAt });
        });

        b.Entity<SessionBooking>(e =>
        {
            e.Property(x => x.Status).HasMaxLength(16);
            e.HasIndex(x => new { x.SessionId, x.MemberProfileId }).IsUnique();
        });

        b.Entity<MemberAttendance>(e =>
        {
            e.Property(x => x.Status).HasMaxLength(32);
            e.Property(x => x.CorrectionReason).HasMaxLength(200);
            e.HasIndex(x => new { x.SessionId, x.MemberProfileId }).IsUnique();
        });

        b.Entity<StaffShift>(e =>
        {
            e.HasIndex(x => new { x.BranchId, x.PersonId, x.Weekday }).IsUnique();
        });

        b.Entity<StaffPunch>(e => e.HasIndex(x => new { x.BranchId, x.PersonId, x.InAt }));

        b.Entity<InboxNote>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(120);
            e.Property(x => x.Body).HasMaxLength(300);
            e.HasIndex(x => new { x.BranchId, x.RecipientPersonId, x.Seen });
        });

        ApplyBranchFilters(b);
    }

    /// <summary>
    /// Every branch-owned row is limited to the selected branch.
    /// With no branch selected the filter matches nothing.
    /// </summary>
    private void ApplyBranchFilters(ModelBuilder b)
    {
        foreach (var entity in b.Model.GetEntityTypes().Where(t => typeof(IBranchOwned).IsAssignableFrom(t.ClrType)))
        {
            var p = Expression.Parameter(entity.ClrType, "e");
            var row = Expression.Convert(Expression.Property(p, nameof(IBranchOwned.BranchId)), typeof(Guid?));
            var current = Expression.Property(Expression.Constant(this), nameof(CurrentBranchId));
            b.Entity(entity.ClrType).HasQueryFilter(BranchFilter, Expression.Lambda(Expression.Equal(row, current), p));
        }

        foreach (var entity in b.Model.GetEntityTypes().Where(t => typeof(ISoftDeletable).IsAssignableFrom(t.ClrType)))
        {
            var p = Expression.Parameter(entity.ClrType, "e");
            var deleted = Expression.Property(p, nameof(ISoftDeletable.DeletedAt));
            var body = Expression.Equal(deleted, Expression.Constant(null, typeof(DateTimeOffset?)));
            b.Entity(entity.ClrType).HasQueryFilter(SoftDeleteFilter, Expression.Lambda(body, p));
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        GuardBranchWrites();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken ct = default)
    {
        GuardBranchWrites();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, ct);
    }

    private void GuardBranchWrites()
    {
        foreach (var entry in ChangeTracker.Entries<IBranchOwned>())
        {
            if (entry.State is EntityState.Unchanged or EntityState.Detached) continue;

            var current = CurrentBranchId
                ?? throw new BranchViolationException($"Write to {entry.Metadata.ClrType.Name} without a branch.");

            if (entry.State == EntityState.Added && entry.Entity.BranchId == Guid.Empty)
                entry.Entity.BranchId = current;

            if (entry.Entity.BranchId != current)
                throw new BranchViolationException($"Write to {entry.Metadata.ClrType.Name} of another branch.");

            if (entry.State == EntityState.Modified && entry.Property(nameof(IBranchOwned.BranchId)).IsModified)
                throw new BranchViolationException("BranchId cannot change.");
        }
    }
}
