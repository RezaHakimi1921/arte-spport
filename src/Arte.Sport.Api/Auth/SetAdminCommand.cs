using Arte.Sport.Core.Clubs;
using Arte.Sport.Core.Data;
using Arte.Sport.Core.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Arte.Sport.Api.Auth;

/// <summary>
/// Server console only. Password comes from SPORT_ADMIN_PASSWORD, never from the API.
/// Usage: set-admin &lt;username&gt;
/// </summary>
public static class SetAdminCommand
{
    public static async Task<int> RunAsync(IServiceProvider services, string[] args)
    {
        if (args.Length != 2 || !Username.TryNormalize(args[1], out var username))
        {
            Console.Error.WriteLine("usage: set-admin <username>");
            return 2;
        }

        var password = Environment.GetEnvironmentVariable("SPORT_ADMIN_PASSWORD") ?? "";
        if (password.Length is < 8 or > 72)
        {
            Console.Error.WriteLine("SPORT_ADMIN_PASSWORD must be 8 to 72 characters.");
            return 2;
        }

        var displayName = Environment.GetEnvironmentVariable("SPORT_ADMIN_NAME")?.Trim() ?? "";
        if (displayName.Length > 80) displayName = displayName[..80];

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SportDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var person = await db.People.SingleOrDefaultAsync(p => p.Username == username);
        if (person is null)
        {
            person = new Person
            {
                Id = Guid.NewGuid(),
                Username = username,
                DisplayName = displayName.Length > 0 ? displayName : username,
                CreatedAt = clock.GetUtcNow(),
            };
            db.People.Add(person);
        }
        else
        {
            if (displayName.Length > 0 && person.DisplayName.Length == 0)
                person.DisplayName = displayName;
        }

        person.IsPlatformAdmin = true;
        person.PasswordHash = new PasswordHasher<Person>().HashPassword(person, password);

        var memberships = await db.Memberships.IgnoreQueryFilters()
            .Where(m => m.PersonId == person.Id && m.IsActive)
            .ToListAsync();
        if (memberships.Count == 0)
        {
            var now = clock.GetUtcNow();
            var brand = new Brand { Id = Guid.NewGuid(), Name = "باشگاه آرتِه", CreatedAt = now };
            var branch = new Branch { Id = Guid.NewGuid(), BrandId = brand.Id, Name = "شعبه مرکزی", CreatedAt = now };
            db.CurrentBranchId = branch.Id;
            db.Brands.Add(brand);
            db.Branches.Add(branch);
            db.Memberships.Add(new Membership
            {
                Id = Guid.NewGuid(),
                PersonId = person.Id,
                BranchId = branch.Id,
                Role = SportRoles.Manager,
                Permissions = SportRoles.DefaultPermissions(SportRoles.Manager),
                CreatedAt = now,
            });
            Console.WriteLine("created the first branch");
        }

        await db.SaveChangesAsync();
        Console.WriteLine($"{username} is a platform admin");
        return 0;
    }
}
