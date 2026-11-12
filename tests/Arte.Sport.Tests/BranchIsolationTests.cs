using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Arte.Sport.Core.Branching;
using Arte.Sport.Core.Data;
using Arte.Sport.Core.Features;
using Arte.Sport.Core.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Arte.Sport.Tests;

[Collection(ApiCollection.Name)]
public sealed class BranchIsolationTests(SportApiFactory api)
{
    [Fact]
    public void Every_branch_owned_entity_has_a_query_filter()
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SportDbContext>();
        var unfiltered = db.Model.GetEntityTypes()
            .Where(t => typeof(IBranchOwned).IsAssignableFrom(t.ClrType))
            .Where(t => t.GetDeclaredQueryFilters().Count == 0)
            .Select(t => t.ClrType.Name)
            .ToList();
        Assert.Empty(unfiltered);
    }

    [Fact]
    public void Receipts_and_AvalAi_stay_off_until_the_core_is_stable()
    {
        using var scope = api.Services.CreateScope();
        var features = scope.ServiceProvider.GetRequiredService<IOptions<FeatureOptions>>().Value;
        var ai = scope.ServiceProvider.GetRequiredService<IOptions<AvalAiOptions>>().Value;
        Assert.False(features.Receipts);
        Assert.False(features.TrainingPlans);
        Assert.False(features.AvalAi);
        Assert.Equal(90, ai.RetentionDays);
        Assert.Equal(0, ai.MonthlyCostCapRials);
        Assert.False(AvalAiGate.CanCall(features, ai));
    }

    [Fact]
    public async Task One_branch_cannot_see_or_change_another_branch_member()
    {
        var (a, _) = await api.NewClubAsync();
        var (b, _) = await api.NewClubAsync();
        var mobile = SportApiFactory.NewMobile();

        var created = await a.PostAsJsonAsync("/api/v1/members", new { fullName = "سارا رضایی", mobile });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.NotFound, (await b.GetAsync($"/api/v1/members/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.PatchAsJsonAsync($"/api/v1/members/{id}", new { fullName = "نفوذ" })).StatusCode);

        var list = await b.GetFromJsonAsync<JsonElement>("/api/v1/members");
        Assert.Equal(0, list.GetArrayLength());

        Assert.Equal(HttpStatusCode.Created, (await b.PostAsJsonAsync("/api/v1/members", new { fullName = "سارا دیگر", mobile })).StatusCode);
        var still = await a.GetFromJsonAsync<JsonElement>($"/api/v1/members/{id}");
        Assert.Equal("سارا رضایی", still.GetProperty("fullName").GetString());
    }

    [Fact]
    public async Task Selecting_a_branch_you_do_not_belong_to_is_refused()
    {
        var (_, branchA) = await api.NewClubAsync();
        var (outsider, _) = await api.LoginAsync();
        var res = await outsider.PostAsJsonAsync("/api/v1/auth/select-branch", new { branchId = branchA });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Without_a_selected_branch_member_routes_are_forbidden()
    {
        var (client, _) = await api.LoginAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/members")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Client().GetAsync("/api/v1/members")).StatusCode);
    }

    [Fact]
    public async Task Coach_sees_only_assigned_members_inside_the_same_branch()
    {
        var (manager, _) = await api.NewClubAsync();
        var coachMobile = SportApiFactory.NewMobile();
        Assert.Equal(HttpStatusCode.Created, (await manager.PostAsJsonAsync("/api/v1/staff", new
        {
            mobile = coachMobile,
            displayName = "مربی یک",
            role = SportRoles.Coach,
        })).StatusCode);

        var first = await manager.PostAsJsonAsync("/api/v1/members", new { fullName = "شاگرد خودش" });
        var second = await manager.PostAsJsonAsync("/api/v1/members", new { fullName = "شاگرد دیگر" });
        var firstId = (await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var secondId = (await second.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var (coach, session) = await api.LoginAsync(coachMobile);
        var coachPersonId = session.GetProperty("personId").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await manager.PostAsJsonAsync($"/api/v1/members/{firstId}/assignment", new { coachPersonId })).StatusCode);

        var visible = await coach.GetFromJsonAsync<JsonElement>("/api/v1/members");
        Assert.Equal(1, visible.GetArrayLength());
        Assert.Equal(firstId, visible[0].GetProperty("id").GetGuid());
        Assert.Equal(HttpStatusCode.NotFound, (await coach.GetAsync($"/api/v1/members/{secondId}")).StatusCode);
    }

    [Fact]
    public async Task Secretary_cannot_grant_a_role_above_herself()
    {
        var (manager, _) = await api.NewClubAsync();
        var secretaryMobile = SportApiFactory.NewMobile();
        Assert.Equal(HttpStatusCode.Created, (await manager.PostAsJsonAsync("/api/v1/staff", new
        {
            mobile = secretaryMobile,
            displayName = "منشی",
            role = SportRoles.Secretary,
        })).StatusCode);

        var (secretary, _) = await api.LoginAsync(secretaryMobile);
        var res = await secretary.PostAsJsonAsync("/api/v1/staff", new
        {
            mobile = SportApiFactory.NewMobile(),
            displayName = "مدیر دوم",
            role = SportRoles.Manager,
        });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Password_login_uses_the_mobile_as_the_username()
    {
        var mobile = SportApiFactory.NewMobile();
        var (client, _) = await api.LoginAsync(mobile);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/v1/auth/password", new { password = "secret-pass" })).StatusCode);

        var fresh = api.Client();
        var login = await fresh.PostAsJsonAsync("/api/v1/auth/login", new { mobile, password = "secret-pass" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var session = await login.Content.ReadFromJsonAsync<JsonElement>();
        fresh.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", session.GetProperty("accessToken").GetString());
        var me = await fresh.GetFromJsonAsync<JsonElement>("/api/v1/me");
        Assert.Equal(mobile, me.GetProperty("mobile").GetString());
        Assert.True(me.GetProperty("hasPassword").GetBoolean());
    }

    [Fact]
    public async Task Username_and_password_sign_in_a_platform_admin()
    {
        var (client, session) = await api.LoginAsync();
        var personId = session.GetProperty("personId").GetGuid();
        await using (var scope = api.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SportDbContext>();
            var person = await db.People.SingleAsync(p => p.Id == personId);
            person.Username = "coach" + personId.ToString("N")[..6];
            person.IsPlatformAdmin = true;
            await db.SaveChangesAsync();
        }

        var meBefore = await client.GetFromJsonAsync<JsonElement>("/api/v1/me");
        var username = meBefore.GetProperty("username").GetString();
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/v1/auth/password", new { password = "secret-pass" })).StatusCode);

        var fresh = api.Client();
        var login = await fresh.PostAsJsonAsync("/api/v1/auth/login", new { username, password = "secret-pass" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        fresh.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", body.GetProperty("accessToken").GetString());
        var me = await fresh.GetFromJsonAsync<JsonElement>("/api/v1/me");
        Assert.True(me.GetProperty("isPlatformAdmin").GetBoolean());
        Assert.Equal(username, me.GetProperty("username").GetString());
    }
}
