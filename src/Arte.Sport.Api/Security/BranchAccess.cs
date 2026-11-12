using System.Security.Claims;
using Arte.Sport.Core.Clubs;
using Arte.Sport.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace Arte.Sport.Api.Security;

public static class SportClaims
{
    public const string PersonId = "sub";
    public const string BranchId = "bid";
    public const string MembershipId = "mid";
}

public sealed class RequestUser
{
    public Guid? PersonId { get; set; }
    public Membership? Membership { get; set; }
}

/// <summary>
/// Re-reads the membership on every request, so a permission change applies immediately.
/// </summary>
public sealed class BranchResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext http, RequestUser user, SportDbContext db)
    {
        if (http.User.Identity?.IsAuthenticated == true)
        {
            if (!Guid.TryParse(http.User.FindFirstValue(SportClaims.PersonId), out var personId))
            {
                http.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            user.PersonId = personId;
            var bid = http.User.FindFirstValue(SportClaims.BranchId);
            var mid = http.User.FindFirstValue(SportClaims.MembershipId);
            if (bid is not null || mid is not null)
            {
                if (!Guid.TryParse(bid, out var branchId) || !Guid.TryParse(mid, out var membershipId))
                {
                    http.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }

                var membership = await db.Memberships.IgnoreQueryFilters().AsNoTracking()
                    .SingleOrDefaultAsync(m => m.Id == membershipId && m.PersonId == personId
                                               && m.BranchId == branchId && m.IsActive, http.RequestAborted);
                if (membership is null)
                {
                    http.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }

                user.Membership = membership;
                db.CurrentBranchId = branchId;
            }
        }

        await next(http);
    }
}

public static class PermissionEndpointExtensions
{
    public static TBuilder RequireBranch<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder =>
        builder.RequireAuthorization().AddEndpointFilter(async (ctx, next) =>
        {
            var user = ctx.HttpContext.RequestServices.GetRequiredService<RequestUser>();
            return user.Membership is null
                ? Results.Problem(statusCode: 403, title: "ابتدا شعبه را انتخاب کنید.")
                : await next(ctx);
        });

    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, params string[] permissions)
        where TBuilder : IEndpointConventionBuilder =>
        builder.RequireBranch().AddEndpointFilter(async (ctx, next) =>
        {
            var membership = ctx.HttpContext.RequestServices.GetRequiredService<RequestUser>().Membership;
            if (membership is null)
                return Results.Problem(statusCode: 403, title: "ابتدا شعبه را انتخاب کنید.");
            if (!permissions.All(membership.Has))
                return Results.Problem(statusCode: 403, title: "دسترسی لازم را ندارید.");
            return await next(ctx);
        });
}
