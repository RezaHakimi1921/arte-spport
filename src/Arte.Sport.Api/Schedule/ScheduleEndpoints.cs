using Arte.Sport.Api.Security;
using Arte.Sport.Core.Data;
using Arte.Sport.Core.Identity;
using Arte.Sport.Core.Schedule;
using Microsoft.EntityFrameworkCore;

namespace Arte.Sport.Api.Schedule;

public static class ScheduleEndpoints
{
    public sealed record SessionInput(string? Title, string? Kind, DateTimeOffset? StartsAt, DateTimeOffset? EndsAt, string? PlaceName, Guid? CoachPersonId, int? Capacity, Guid[]? MemberIds);
    public sealed record BookingInput(Guid? MemberId);
    public sealed record AttendanceInput(Guid? MemberId, string? Status, string? Reason);
    public sealed record ShiftInput(Guid? PersonId, int? Weekday, string? StartsAt, string? EndsAt);
    public sealed record ClosePunchInput(string? Reason);

    public static void Map(WebApplication app)
    {
        var g = app.MapGroup("/api/v1/sessions").RequireBranch();
        g.MapGet("/", ListAsync);
        g.MapGet("/{id:guid}", GetAsync);
        g.MapPost("/", CreateAsync).RequirePermission(SportPermissions.SessionsManage);
        g.MapPost("/{id:guid}/bookings", BookAsync).RequirePermission(SportPermissions.SessionsManage);
        g.MapPost("/{id:guid}/attendance", AttendAsync).RequirePermission(SportPermissions.AttendanceRecord);

        app.MapGet("/api/v1/me/sessions", MineAsync).RequireBranch();
        app.MapPost("/api/v1/me/sessions/{id:guid}/cancel", CancelMineAsync).RequireBranch();
        app.MapGet("/api/v1/inbox", InboxAsync).RequireBranch();
        app.MapPost("/api/v1/inbox/{id:guid}/seen", SeeAsync).RequireBranch();
        app.MapPost("/api/v1/shifts", SetShiftAsync).RequirePermission(SportPermissions.BranchSettings);
        app.MapGet("/api/v1/shifts", ListShiftsAsync).RequirePermission(SportPermissions.BranchSettings);
        app.MapPost("/api/v1/punches/in", PunchInAsync).RequireBranch();
        app.MapPost("/api/v1/punches/out", PunchOutAsync).RequireBranch();
        app.MapPost("/api/v1/punches/{id:guid}/close", ClosePunchAsync).RequirePermission(SportPermissions.BranchSettings);
    }

    private static async Task<IResult> ListAsync(DateTimeOffset? from, DateTimeOffset? to, RequestUser me, SportDbContext db, CancellationToken ct)
    {
        var start = from ?? DateTimeOffset.UtcNow.AddDays(-1);
        var end = to ?? start.AddDays(8);
        var query = db.Sessions.AsNoTracking().Where(s => s.StartsAt < end && s.EndsAt > start);
        if (!me.Membership!.Has(SportPermissions.SessionsManage) && !me.Membership.Has(SportPermissions.MembersRead))
        {
            if (me.Membership.Role == SportRoles.Coach)
                query = query.Where(s => s.CoachPersonId == me.PersonId);
            else
            {
                var mine = await MyMemberId(db, me, ct);
                if (mine is null) return Results.Ok(Array.Empty<object>());
                var ids = db.Bookings.Where(b => b.MemberProfileId == mine && b.Status == "booked").Select(b => b.SessionId);
                query = query.Where(s => ids.Contains(s.Id));
            }
        }

        var rows = await query.OrderBy(s => s.StartsAt).ToListAsync(ct);
        return Results.Ok(rows.Select(SessionDto));
    }

    private static async Task<IResult> GetAsync(Guid id, SportDbContext db, CancellationToken ct)
    {
        var session = await db.Sessions.AsNoTracking().SingleOrDefaultAsync(s => s.Id == id, ct);
        return session is null ? Results.Problem(statusCode: 404, title: "یافت نشد.") : Results.Ok(SessionDto(session));
    }

    private static async Task<IResult> CreateAsync(SessionInput input, SportDbContext db, CancellationToken ct)
    {
        var title = input.Title?.Trim() ?? "";
        if (title.Length is 0 or > 80) return Results.Problem(statusCode: 400, title: "عنوان جلسه لازم است.");
        if (input.StartsAt is null || input.EndsAt is null || input.EndsAt <= input.StartsAt)
            return Results.Problem(statusCode: 400, title: "ساعت شروع و پایان درست نیست.");
        if (input.CoachPersonId is null || !await db.Memberships.AnyAsync(m => m.PersonId == input.CoachPersonId && m.IsActive, ct))
            return Results.Problem(statusCode: 400, title: "مربی این شعبه نیست.");
        var kind = input.Kind == "group" ? "group" : "private";
        var place = string.IsNullOrWhiteSpace(input.PlaceName) ? null : input.PlaceName.Trim();
        if (place is { Length: > 80 }) return Results.Problem(statusCode: 400, title: "نام فضا طولانی است.");

        var clash = await ClashAsync(db, null, input.StartsAt.Value, input.EndsAt.Value, input.CoachPersonId.Value, place, input.MemberIds ?? [], ct);
        if (clash is not null) return Results.Problem(statusCode: 400, title: clash);

        var session = new GymSession
        {
            Id = Guid.NewGuid(),
            Title = title,
            Kind = kind,
            StartsAt = input.StartsAt.Value,
            EndsAt = input.EndsAt.Value,
            PlaceName = place,
            CoachPersonId = input.CoachPersonId.Value,
            Capacity = kind == "group" ? input.Capacity : 1,
            Status = "published",
        };
        db.Sessions.Add(session);
        foreach (var memberId in input.MemberIds ?? [])
        {
            if (!await db.Members.AnyAsync(m => m.Id == memberId, ct))
                return Results.Problem(statusCode: 400, title: "یکی از شاگردها در این شعبه نیست.");
            db.Bookings.Add(new SessionBooking { Id = Guid.NewGuid(), SessionId = session.Id, MemberProfileId = memberId });
        }
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/sessions/{session.Id}", SessionDto(session));
    }

    private static async Task<IResult> BookAsync(Guid id, BookingInput input, SportDbContext db, CancellationToken ct)
    {
        var session = await db.Sessions.SingleOrDefaultAsync(s => s.Id == id && s.Status != "cancelled", ct);
        if (session is null || input.MemberId is null) return Results.Problem(statusCode: 404, title: "یافت نشد.");
        if (!await db.Members.AnyAsync(m => m.Id == input.MemberId, ct))
            return Results.Problem(statusCode: 400, title: "شاگرد این شعبه نیست.");
        var clash = await ClashAsync(db, id, session.StartsAt, session.EndsAt, session.CoachPersonId, session.PlaceName, [input.MemberId.Value], ct);
        if (clash is not null) return Results.Problem(statusCode: 400, title: clash);
        var existing = await db.Bookings.SingleOrDefaultAsync(b => b.SessionId == id && b.MemberProfileId == input.MemberId, ct);
        if (existing is null)
            db.Bookings.Add(new SessionBooking { Id = Guid.NewGuid(), SessionId = id, MemberProfileId = input.MemberId.Value });
        else
        {
            existing.Status = "booked";
            existing.CancelledAt = null;
        }
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> MineAsync(RequestUser me, SportDbContext db, CancellationToken ct)
    {
        var memberId = await MyMemberId(db, me, ct);
        if (memberId is null) return Results.Ok(Array.Empty<object>());
        var ids = db.Bookings.Where(b => b.MemberProfileId == memberId).Select(b => new { b.SessionId, b.Status });
        var rows = await db.Sessions.Where(s => ids.Select(i => i.SessionId).Contains(s.Id)).OrderBy(s => s.StartsAt).ToListAsync(ct);
        var states = await ids.ToListAsync(ct);
        return Results.Ok(rows.Select(s => new
        {
            s.Id, s.Title, s.Kind, s.StartsAt, s.EndsAt, s.PlaceName, s.Status,
            booking = states.First(x => x.SessionId == s.Id).Status,
        }));
    }

    private static async Task<IResult> CancelMineAsync(Guid id, RequestUser me, SportDbContext db, TimeProvider clock, CancellationToken ct)
    {
        var branch = await db.Branches.SingleAsync(b => b.Id == me.Membership!.BranchId, ct);
        if (!branch.MemberCanCancel) return Results.Problem(statusCode: 403, title: "لغو توسط شاگرد در این شعبه خاموش است.");
        var memberId = await MyMemberId(db, me, ct);
        var booking = memberId is null ? null : await db.Bookings.SingleOrDefaultAsync(b => b.SessionId == id && b.MemberProfileId == memberId && b.Status == "booked", ct);
        var session = booking is null ? null : await db.Sessions.SingleOrDefaultAsync(s => s.Id == id && s.Status != "cancelled", ct);
        if (booking is null || session is null) return Results.Problem(statusCode: 404, title: "یافت نشد.");
        var now = clock.GetUtcNow();
        if (now > session.StartsAt.AddMinutes(-branch.CancelUntilMinutes))
            return Results.Problem(statusCode: 400, title: "مهلت لغو این جلسه گذشته است.");

        booking.Status = "cancelled";
        booking.CancelledAt = now;
        var attendance = await db.Attendances.SingleOrDefaultAsync(a => a.SessionId == id && a.MemberProfileId == memberId, ct);
        if (attendance is null)
        {
            db.Attendances.Add(new MemberAttendance
            {
                Id = Guid.NewGuid(),
                SessionId = id,
                MemberProfileId = memberId!.Value,
                Status = "cancelled_informed",
                RecordedBy = me.PersonId!.Value,
                RecordedAt = now,
            });
        }
        else attendance.Status = "cancelled_informed";

        var member = await db.Members.SingleAsync(m => m.Id == memberId, ct);
        var managers = await db.Memberships.Where(m => m.IsActive && m.Role == SportRoles.Manager).Select(m => m.PersonId).ToListAsync(ct);
        foreach (var personId in managers)
        {
            db.Inbox.Add(new InboxNote
            {
                Id = Guid.NewGuid(),
                RecipientPersonId = personId,
                Title = "لغو جلسه",
                Body = $"{member.FullName} جلسهٔ {session.Title} را لغو کرد.",
                CreatedAt = now,
            });
        }
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> AttendAsync(Guid id, AttendanceInput input, RequestUser me, SportDbContext db, TimeProvider clock, CancellationToken ct)
    {
        if (input.MemberId is null) return Results.Problem(statusCode: 400, title: "شاگرد مشخص نیست.");
        var allowed = new[] { "present", "absent", "cancelled_informed", "cancelled_uninformed" };
        if (input.Status is null || !allowed.Contains(input.Status))
            return Results.Problem(statusCode: 400, title: "وضعیت حضور شناخته نیست.");
        if (!await db.Sessions.AnyAsync(s => s.Id == id, ct) || !await db.Members.AnyAsync(m => m.Id == input.MemberId, ct))
            return Results.Problem(statusCode: 404, title: "یافت نشد.");
        var row = await db.Attendances.SingleOrDefaultAsync(a => a.SessionId == id && a.MemberProfileId == input.MemberId, ct);
        if (row is null)
        {
            db.Attendances.Add(new MemberAttendance
            {
                Id = Guid.NewGuid(),
                SessionId = id,
                MemberProfileId = input.MemberId.Value,
                Status = input.Status,
                RecordedBy = me.PersonId!.Value,
                RecordedAt = clock.GetUtcNow(),
            });
        }
        else
        {
            if (string.IsNullOrWhiteSpace(input.Reason))
                return Results.Problem(statusCode: 400, title: "اصلاح حضور علت می‌خواهد.");
            row.Status = input.Status;
            row.CorrectionReason = input.Reason.Trim();
            row.RecordedBy = me.PersonId!.Value;
            row.RecordedAt = clock.GetUtcNow();
        }
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> InboxAsync(RequestUser me, SportDbContext db, CancellationToken ct)
    {
        var rows = await db.Inbox.AsNoTracking()
            .Where(n => n.RecipientPersonId == me.PersonId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new { n.Id, n.Title, n.Body, n.Seen, n.CreatedAt })
            .ToListAsync(ct);
        return Results.Ok(rows);
    }

    private static async Task<IResult> SeeAsync(Guid id, RequestUser me, SportDbContext db, CancellationToken ct)
    {
        var note = await db.Inbox.SingleOrDefaultAsync(n => n.Id == id && n.RecipientPersonId == me.PersonId, ct);
        if (note is null) return Results.Problem(statusCode: 404, title: "یافت نشد.");
        note.Seen = true;
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> SetShiftAsync(ShiftInput input, SportDbContext db, CancellationToken ct)
    {
        if (input.PersonId is null || input.Weekday is null or < 0 or > 6)
            return Results.Problem(statusCode: 400, title: "شخص و روز هفته لازم است.");
        if (!TimeOnly.TryParse(input.StartsAt, out var start) || !TimeOnly.TryParse(input.EndsAt, out var end) || end <= start)
            return Results.Problem(statusCode: 400, title: "ساعت شیفت درست نیست.");
        if (!await db.Memberships.AnyAsync(m => m.PersonId == input.PersonId && m.IsActive, ct))
            return Results.Problem(statusCode: 400, title: "این شخص در شعبه نیست.");
        var row = await db.StaffShifts.SingleOrDefaultAsync(s => s.PersonId == input.PersonId && s.Weekday == input.Weekday, ct);
        if (row is null)
        {
            db.StaffShifts.Add(new StaffShift
            {
                Id = Guid.NewGuid(),
                PersonId = input.PersonId.Value,
                Weekday = input.Weekday.Value,
                StartsAt = start,
                EndsAt = end,
            });
        }
        else
        {
            row.StartsAt = start;
            row.EndsAt = end;
        }
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ListShiftsAsync(SportDbContext db, CancellationToken ct)
    {
        var rows = await db.StaffShifts.AsNoTracking()
            .Select(s => new { s.Id, s.PersonId, s.Weekday, startsAt = s.StartsAt.ToString("HH:mm"), endsAt = s.EndsAt.ToString("HH:mm") })
            .ToListAsync(ct);
        return Results.Ok(rows);
    }

    private static async Task<IResult> PunchInAsync(RequestUser me, SportDbContext db, TimeProvider clock, CancellationToken ct)
    {
        if (await db.StaffPunches.AnyAsync(p => p.PersonId == me.PersonId && p.OutAt == null, ct))
            return Results.Problem(statusCode: 409, title: "یک ورود باز دارید. اول خروج را بزنید.");
        db.StaffPunches.Add(new StaffPunch { Id = Guid.NewGuid(), PersonId = me.PersonId!.Value, InAt = clock.GetUtcNow() });
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> PunchOutAsync(RequestUser me, SportDbContext db, TimeProvider clock, CancellationToken ct)
    {
        var open = await db.StaffPunches.Where(p => p.PersonId == me.PersonId && p.OutAt == null).OrderByDescending(p => p.InAt).FirstOrDefaultAsync(ct);
        if (open is null) return Results.Problem(statusCode: 404, title: "ورود بازی ندارید.");
        open.OutAt = clock.GetUtcNow();
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ClosePunchAsync(Guid id, ClosePunchInput input, SportDbContext db, TimeProvider clock, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Reason))
            return Results.Problem(statusCode: 400, title: "بستن شیفت فراموش‌شده علت می‌خواهد.");
        var open = await db.StaffPunches.SingleOrDefaultAsync(p => p.Id == id && p.OutAt == null, ct);
        if (open is null) return Results.Problem(statusCode: 404, title: "یافت نشد.");
        open.OutAt = clock.GetUtcNow();
        open.CloseReason = input.Reason.Trim();
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<Guid?> MyMemberId(SportDbContext db, RequestUser me, CancellationToken ct)
    {
        var mobile = await db.People.Where(p => p.Id == me.PersonId).Select(p => p.Mobile).SingleAsync(ct);
        return await db.Members.Where(m => m.Mobile == mobile).Select(m => (Guid?)m.Id).FirstOrDefaultAsync(ct);
    }

    private static async Task<string?> ClashAsync(
        SportDbContext db, Guid? ignoreSession, DateTimeOffset start, DateTimeOffset end, Guid coachId, string? place, IReadOnlyCollection<Guid> memberIds, CancellationToken ct)
    {
        var overlap = db.Sessions.Where(s => s.Status != "cancelled" && s.Id != ignoreSession && s.StartsAt < end && s.EndsAt > start);
        if (await overlap.AnyAsync(s => s.CoachPersonId == coachId, ct))
            return "این مربی در این ساعت جلسهٔ دیگری دارد.";
        if (!string.IsNullOrEmpty(place) && await overlap.AnyAsync(s => s.PlaceName == place, ct))
            return "این فضا در این ساعت پر است.";
        if (memberIds.Count == 0) return null;
        var busy = await db.Bookings
            .Where(b => b.Status == "booked" && memberIds.Contains(b.MemberProfileId) && overlap.Select(s => s.Id).Contains(b.SessionId))
            .AnyAsync(ct);
        return busy ? "یکی از شاگردها در این ساعت جلسهٔ دیگری دارد." : null;
    }

    private static object SessionDto(GymSession s) => new
    {
        s.Id, s.Title, s.Kind, s.StartsAt, s.EndsAt, s.PlaceName, s.CoachPersonId, s.Status, s.Capacity,
    };
}
