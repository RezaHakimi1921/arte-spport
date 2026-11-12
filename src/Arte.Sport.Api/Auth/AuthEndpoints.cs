using Arte.Sport.Api.Security;
using Arte.Sport.Api.Sms;
using Arte.Sport.Core.Clubs;
using Arte.Sport.Core.Data;
using Arte.Sport.Core.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Arte.Sport.Api.Auth;

public static class AuthEndpoints
{
    public sealed record MobileInput(string? Mobile);
    public sealed record VerifyInput(string? Mobile, string? Code);
    public sealed record PasswordLoginInput(string? Mobile, string? Username, string? Password);
    public sealed record SetPasswordInput(string? Password);
    public sealed record SelectBranchInput(Guid? BranchId);

    public static void Map(WebApplication app)
    {
        var g = app.MapGroup("/api/v1/auth");
        g.MapPost("/otp/request", RequestOtpAsync);
        g.MapPost("/otp/verify", VerifyOtpAsync);
        g.MapPost("/login", PasswordLoginAsync);
        g.MapPost("/password", SetPasswordAsync).RequireAuthorization();
        g.MapPost("/refresh", RefreshAsync).RequireAuthorization();
        g.MapPost("/select-branch", SelectBranchAsync).RequireAuthorization();
        app.MapGet("/api/v1/me", MeAsync).RequireAuthorization();
    }

    private static async Task<IResult> RequestOtpAsync(MobileInput input, OtpService otp, CancellationToken ct)
    {
        if (!Mobile.TryNormalize(input.Mobile, out var mobile))
            return Results.Problem(statusCode: 400, title: "شماره موبایل درست نیست.");
        try
        {
            await otp.RequestAsync(mobile, ct);
        }
        catch (SmsNotConfiguredException)
        {
            return Results.Problem(statusCode: 503, title: "ارسال پیامک هنوز پیکربندی نشده.");
        }
        return Results.NoContent();
    }

    private static async Task<IResult> VerifyOtpAsync(VerifyInput input, OtpService otp, SportDbContext db, TokenService tokens, TimeProvider clock, CancellationToken ct)
    {
        if (!Mobile.TryNormalize(input.Mobile, out var mobile) || string.IsNullOrWhiteSpace(input.Code))
            return Results.Problem(statusCode: 400, title: "کد یا شماره درست نیست.");
        if (!await otp.VerifyAsync(mobile, input.Code.Trim(), ct))
            return Results.Problem(statusCode: 401, title: "کد نادرست یا منقضی است.");

        var person = await FindOrCreatePersonAsync(db, mobile, clock, ct);
        return await SessionAsync(db, tokens, person, ct);
    }

    private static async Task<IResult> PasswordLoginAsync(PasswordLoginInput input, SportDbContext db, TokenService tokens, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(input.Password))
            return Results.Problem(statusCode: 400, title: "شماره یا رمز درست نیست.");
        var raw = string.IsNullOrWhiteSpace(input.Username) ? input.Mobile : input.Username;
        Person? person = null;
        if (Mobile.TryNormalize(raw, out var mobile))
            person = await db.People.SingleOrDefaultAsync(p => p.Mobile == mobile, ct);
        else if (Arte.Sport.Core.Identity.Username.TryNormalize(raw, out var username))
            person = await db.People.SingleOrDefaultAsync(p => p.Username == username, ct);
        else
            return Results.Problem(statusCode: 400, title: "شماره یا رمز درست نیست.");
        if (person?.PasswordHash is null)
            return Results.Problem(statusCode: 401, title: "ورود با رمز برای این حساب فعال نیست.");
        var verdict = new PasswordHasher<Person>().VerifyHashedPassword(person, person.PasswordHash, input.Password);
        if (verdict == PasswordVerificationResult.Failed)
            return Results.Problem(statusCode: 401, title: "شماره یا رمز درست نیست.");
        return await SessionAsync(db, tokens, person, ct);
    }

    private static async Task<IResult> SetPasswordAsync(SetPasswordInput input, RequestUser me, SportDbContext db, CancellationToken ct)
    {
        if (input.Password is null || input.Password.Length < 8 || input.Password.Length > 72)
            return Results.Problem(statusCode: 400, title: "رمز باید حداقل ۸ نویسه باشد.");
        var person = await db.People.SingleAsync(p => p.Id == me.PersonId, ct);
        person.PasswordHash = new PasswordHasher<Person>().HashPassword(person, input.Password);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> RefreshAsync(RequestUser me, SportDbContext db, TokenService tokens, CancellationToken ct)
    {
        var person = await db.People.SingleAsync(p => p.Id == me.PersonId, ct);
        Membership? membership = null;
        if (me.Membership is not null)
        {
            membership = await db.Memberships.IgnoreQueryFilters()
                .SingleOrDefaultAsync(m => m.Id == me.Membership.Id && m.IsActive && m.PersonId == person.Id, ct);
            if (membership is null)
                return Results.Problem(statusCode: 401, title: "عضویت این شعبه دیگر فعال نیست.");
        }
        return Results.Ok(SessionBody(tokens, person, membership));
    }

    private static async Task<IResult> SelectBranchAsync(SelectBranchInput input, RequestUser me, SportDbContext db, TokenService tokens, CancellationToken ct)
    {
        if (input.BranchId is null) return Results.Problem(statusCode: 400, title: "شعبه مشخص نیست.");
        var membership = await db.Memberships.IgnoreQueryFilters()
            .SingleOrDefaultAsync(m => m.PersonId == me.PersonId && m.BranchId == input.BranchId && m.IsActive, ct);
        if (membership is null)
            return Results.Problem(statusCode: 403, title: "به این شعبه دسترسی ندارید.");
        var person = await db.People.SingleAsync(p => p.Id == me.PersonId, ct);
        return Results.Ok(SessionBody(tokens, person, membership));
    }

    private static async Task<IResult> MeAsync(RequestUser me, SportDbContext db, CancellationToken ct)
    {
        var person = await db.People.AsNoTracking().SingleAsync(p => p.Id == me.PersonId, ct);
        var rows = await db.Memberships.IgnoreQueryFilters().AsNoTracking()
            .Where(m => m.PersonId == person.Id && m.IsActive)
            .Join(db.Branches.AsNoTracking(), m => m.BranchId, b => b.Id, (m, b) => new
            {
                m.BranchId,
                branchName = b.Name,
                m.Role,
            })
            .ToListAsync(ct);
        return Results.Ok(new
        {
            person.Id,
            person.Mobile,
            person.Username,
            person.DisplayName,
            person.IsPlatformAdmin,
            hasPassword = person.PasswordHash is not null,
            branchId = me.Membership?.BranchId,
            role = me.Membership?.Role,
            permissions = me.Membership?.Permissions ?? [],
            branches = rows,
        });
    }

    public static async Task<Person> FindOrCreatePersonAsync(SportDbContext db, string mobile, TimeProvider clock, CancellationToken ct)
    {
        var person = await db.People.SingleOrDefaultAsync(p => p.Mobile == mobile, ct);
        if (person is not null) return person;
        person = new Person { Id = Guid.NewGuid(), Mobile = mobile, DisplayName = "", CreatedAt = clock.GetUtcNow() };
        db.People.Add(person);
        await db.SaveChangesAsync(ct);
        return person;
    }

    private static async Task<IResult> SessionAsync(SportDbContext db, TokenService tokens, Person person, CancellationToken ct)
    {
        var memberships = await db.Memberships.IgnoreQueryFilters()
            .Where(m => m.PersonId == person.Id && m.IsActive)
            .ToListAsync(ct);
        var selected = memberships.Count == 1 ? memberships[0] : null;
        return Results.Ok(SessionBody(tokens, person, selected));
    }

    public static object SessionBody(TokenService tokens, Person person, Membership? membership) => new
    {
        accessToken = tokens.Issue(person.Id, membership),
        personId = person.Id,
        branchId = membership?.BranchId,
        role = membership?.Role,
    };
}
