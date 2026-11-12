using Arte.Sport.Api.Auth;
using Arte.Sport.Api.Security;
using Arte.Sport.Core.Clubs;
using Arte.Sport.Core.Data;
using Arte.Sport.Core.Identity;
using Microsoft.EntityFrameworkCore;

namespace Arte.Sport.Api.Clubs;

public static class ClubEndpoints
{
    public sealed record CreateBrandInput(string? Name, string? BranchName, string? OwnerName);
    public sealed record StaffInput(string? Mobile, string? DisplayName, string? Role);

    public static void Map(WebApplication app)
    {
        app.MapPost("/api/v1/brands", CreateBrandAsync).RequireAuthorization();
        app.MapPost("/api/v1/staff", AddStaffAsync).RequirePermission(SportPermissions.StaffManage);
        app.MapGet("/api/v1/staff", ListStaffAsync).RequirePermission(SportPermissions.StaffManage);
    }

    private static async Task<IResult> CreateBrandAsync(
        CreateBrandInput input, RequestUser me, SportDbContext db, TokenService tokens, TimeProvider clock, CancellationToken ct)
    {
        var name = input.Name?.Trim() ?? "";
        var branchName = input.BranchName?.Trim() ?? "";
        if (name.Length is 0 or > 80 || branchName.Length is 0 or > 80)
            return Results.Problem(statusCode: 400, title: "نام مجموعه و شعبه لازم است.");

        var now = clock.GetUtcNow();
        var brand = new Brand { Id = Guid.NewGuid(), Name = name, CreatedAt = now };
        var branch = new Branch { Id = Guid.NewGuid(), BrandId = brand.Id, Name = branchName, CreatedAt = now };
        var membership = new Membership
        {
            Id = Guid.NewGuid(),
            PersonId = me.PersonId!.Value,
            BranchId = branch.Id,
            Role = SportRoles.Manager,
            Permissions = SportRoles.DefaultPermissions(SportRoles.Manager),
            CreatedAt = now,
        };

        var person = await db.People.SingleAsync(p => p.Id == me.PersonId, ct);
        var ownerName = input.OwnerName?.Trim() ?? "";
        if (ownerName.Length is > 0 and <= 80 && person.DisplayName.Length == 0)
            person.DisplayName = ownerName;

        db.CurrentBranchId = branch.Id;
        db.Brands.Add(brand);
        db.Branches.Add(branch);
        db.Memberships.Add(membership);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/brands/{brand.Id}", new
        {
            id = brand.Id,
            branchId = branch.Id,
            accessToken = tokens.Issue(person.Id, membership),
        });
    }

    private static async Task<IResult> AddStaffAsync(
        StaffInput input, RequestUser me, SportDbContext db, TimeProvider clock, CancellationToken ct)
    {
        if (!Mobile.TryNormalize(input.Mobile, out var mobile))
            return Results.Problem(statusCode: 400, title: "شماره موبایل درست نیست.");
        var role = input.Role?.Trim() ?? "";
        if (!SportRoles.All.Contains(role))
            return Results.Problem(statusCode: 400, title: "نقش شناخته نیست.");
        var permissions = SportRoles.DefaultPermissions(role);
        if (!permissions.All(me.Membership!.Has))
            return Results.Problem(statusCode: 403, title: "نمی‌توانید دسترسی‌ای بدهید که خودتان ندارید.");

        var displayName = input.DisplayName?.Trim() ?? "";
        if (displayName.Length > 80)
            return Results.Problem(statusCode: 400, title: "نام طولانی است.");

        var person = await AuthEndpoints.FindOrCreatePersonAsync(db, mobile, clock, ct);
        if (displayName.Length > 0 && person.DisplayName.Length == 0)
            person.DisplayName = displayName;

        var exists = await db.Memberships.IgnoreQueryFilters()
            .AnyAsync(m => m.PersonId == person.Id && m.BranchId == me.Membership.BranchId, ct);
        if (exists)
            return Results.Problem(statusCode: 409, title: "این شماره همین حالا عضو این شعبه است.");

        var membership = new Membership
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            BranchId = me.Membership.BranchId,
            Role = role,
            Permissions = permissions,
            CreatedAt = clock.GetUtcNow(),
        };
        db.Memberships.Add(membership);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/staff/{membership.Id}", new
        {
            membership.Id,
            person.Mobile,
            person.DisplayName,
            membership.Role,
        });
    }

    private static async Task<IResult> ListStaffAsync(SportDbContext db, CancellationToken ct)
    {
        var rows = await db.Memberships.AsNoTracking()
            .Join(db.People.AsNoTracking(), m => m.PersonId, p => p.Id, (m, p) => new
            {
                m.Id,
                m.PersonId,
                p.Mobile,
                p.DisplayName,
                m.Role,
                m.Permissions,
            })
            .ToListAsync(ct);
        return Results.Ok(rows);
    }
}
