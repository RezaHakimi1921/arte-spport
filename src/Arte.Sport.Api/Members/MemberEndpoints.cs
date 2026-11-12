using Arte.Sport.Api.Auth;
using Arte.Sport.Api.Security;
using Arte.Sport.Core.Clubs;
using Arte.Sport.Core.Data;
using Arte.Sport.Core.Identity;
using Arte.Sport.Core.Members;
using Microsoft.EntityFrameworkCore;

namespace Arte.Sport.Api.Members;

public static class MemberEndpoints
{
    public sealed record CreateMemberInput(string? FullName, string? Mobile);
    public sealed record PatchMemberInput(string? FullName);
    public sealed record AssignInput(Guid? CoachPersonId);

    public static void Map(WebApplication app)
    {
        var g = app.MapGroup("/api/v1/members");
        g.MapPost("/", CreateAsync).RequirePermission(SportPermissions.MembersManage);
        g.MapGet("/", ListAsync).RequireBranch();
        g.MapGet("/{id:guid}", GetAsync).RequireBranch();
        g.MapPatch("/{id:guid}", PatchAsync).RequirePermission(SportPermissions.MembersManage);
        g.MapPost("/{id:guid}/assignment", AssignAsync).RequirePermission(SportPermissions.MembersManage);
    }

    private static async Task<IResult> CreateAsync(
        CreateMemberInput input, RequestUser me, SportDbContext db, TimeProvider clock, CancellationToken ct)
    {
        var name = input.FullName?.Trim() ?? "";
        if (name.Length is 0 or > 80)
            return Results.Problem(statusCode: 400, title: "نام شاگرد لازم است.");
        string? mobile = null;
        if (!string.IsNullOrWhiteSpace(input.Mobile))
        {
            if (!Mobile.TryNormalize(input.Mobile, out var normalized))
                return Results.Problem(statusCode: 400, title: "شماره موبایل درست نیست.");
            mobile = normalized;
        }

        var member = new MemberProfile
        {
            Id = Guid.NewGuid(),
            FullName = name,
            Mobile = mobile,
            CreatedAt = clock.GetUtcNow(),
        };
        db.Members.Add(member);
        if (mobile is not null)
        {
            var person = await AuthEndpoints.FindOrCreatePersonAsync(db, mobile, clock, ct);
            if (person.DisplayName.Length == 0) person.DisplayName = name;
            var already = await db.Memberships.AnyAsync(m => m.PersonId == person.Id, ct);
            if (!already)
            {
                db.Memberships.Add(new Membership
                {
                    Id = Guid.NewGuid(),
                    PersonId = person.Id,
                    BranchId = me.Membership!.BranchId,
                    Role = SportRoles.Member,
                    Permissions = SportRoles.DefaultPermissions(SportRoles.Member),
                    CreatedAt = clock.GetUtcNow(),
                });
            }
        }
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return Results.Problem(statusCode: 409, title: "این شماره در این شعبه ثبت شده است.");
        }
        return Results.Created($"/api/v1/members/{member.Id}", ToDto(member));
    }

    private static async Task<IResult> ListAsync(RequestUser me, SportDbContext db, CancellationToken ct)
    {
        var query = Visible(db, me);
        if (query is null) return Results.Problem(statusCode: 403, title: "دسترسی لازم را ندارید.");
        var rows = await query.OrderBy(m => m.FullName).Select(m => new { m.Id, m.FullName, m.Mobile, m.MembershipStatus }).ToListAsync(ct);
        return Results.Ok(rows);
    }

    private static async Task<IResult> GetAsync(Guid id, RequestUser me, SportDbContext db, CancellationToken ct)
    {
        var query = Visible(db, me);
        if (query is null) return Results.Problem(statusCode: 403, title: "دسترسی لازم را ندارید.");
        var member = await query.SingleOrDefaultAsync(m => m.Id == id, ct);
        return member is null ? Results.Problem(statusCode: 404, title: "یافت نشد.") : Results.Ok(ToDto(member));
    }

    private static async Task<IResult> PatchAsync(Guid id, PatchMemberInput input, SportDbContext db, CancellationToken ct)
    {
        var member = await db.Members.SingleOrDefaultAsync(m => m.Id == id, ct);
        if (member is null) return Results.Problem(statusCode: 404, title: "یافت نشد.");
        var name = input.FullName?.Trim() ?? "";
        if (name.Length is 0 or > 80)
            return Results.Problem(statusCode: 400, title: "نام شاگرد لازم است.");
        member.FullName = name;
        await db.SaveChangesAsync(ct);
        return Results.Ok(ToDto(member));
    }

    private static async Task<IResult> AssignAsync(
        Guid id, AssignInput input, SportDbContext db, TimeProvider clock, CancellationToken ct)
    {
        if (input.CoachPersonId is null)
            return Results.Problem(statusCode: 400, title: "مربی مشخص نیست.");
        var member = await db.Members.SingleOrDefaultAsync(m => m.Id == id, ct);
        if (member is null) return Results.Problem(statusCode: 404, title: "یافت نشد.");
        var coachOk = await db.Memberships.AnyAsync(m => m.PersonId == input.CoachPersonId && m.IsActive, ct);
        if (!coachOk)
            return Results.Problem(statusCode: 400, title: "مربی این شعبه نیست.");

        var now = clock.GetUtcNow();
        var open = await db.Assignments.Where(a => a.MemberProfileId == id && a.EndsAt == null).ToListAsync(ct);
        foreach (var row in open) row.EndsAt = now;
        db.Assignments.Add(new Assignment
        {
            Id = Guid.NewGuid(),
            MemberProfileId = id,
            CoachPersonId = input.CoachPersonId.Value,
            StartsAt = now,
        });
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static IQueryable<MemberProfile>? Visible(SportDbContext db, RequestUser me)
    {
        var membership = me.Membership!;
        if (membership.Has(SportPermissions.MembersRead) || membership.Has(SportPermissions.MembersManage))
            return db.Members;
        if (!membership.Has(SportPermissions.MembersReadAssigned)) return null;
        var mine = db.Assignments
            .Where(a => a.CoachPersonId == me.PersonId && a.EndsAt == null)
            .Select(a => a.MemberProfileId);
        return db.Members.Where(m => mine.Contains(m.Id));
    }

    private static object ToDto(MemberProfile member) => new
    {
        member.Id,
        member.FullName,
        member.Mobile,
        member.MembershipStatus,
    };
}
