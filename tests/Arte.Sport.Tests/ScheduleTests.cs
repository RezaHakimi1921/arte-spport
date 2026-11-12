using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Arte.Sport.Tests;

[Collection(ApiCollection.Name)]
public sealed class ScheduleTests(SportApiFactory api)
{
    [Fact]
    public async Task Overlapping_coach_is_rejected_and_another_branch_cannot_see_the_session()
    {
        var (club, _) = await api.NewClubAsync();
        var me = await club.GetFromJsonAsync<JsonElement>("/api/v1/me");
        var coachId = me.GetProperty("id").GetGuid();
        var start = DateTimeOffset.UtcNow.AddHours(2);
        var created = await club.PostAsJsonAsync("/api/v1/sessions", new
        {
            title = "خصوصی",
            kind = "private",
            startsAt = start,
            endsAt = start.AddHours(1),
            placeName = "سالن ۱",
            coachPersonId = coachId,
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var again = await club.PostAsJsonAsync("/api/v1/sessions", new
        {
            title = "تداخل",
            kind = "private",
            startsAt = start.AddMinutes(15),
            endsAt = start.AddMinutes(45),
            placeName = "سالن ۲",
            coachPersonId = coachId,
        });
        Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode);

        var (other, _) = await api.NewClubAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/v1/sessions/{id}")).StatusCode);
        var list = await other.GetFromJsonAsync<JsonElement>($"/api/v1/sessions?from={Uri.EscapeDataString(start.AddHours(-1).ToString("O"))}&to={Uri.EscapeDataString(start.AddHours(3).ToString("O"))}");
        Assert.Equal(0, list.GetArrayLength());
    }

    [Fact]
    public async Task Member_cancel_keeps_the_group_session_and_notifies_the_manager()
    {
        var (club, _) = await api.NewClubAsync();
        var me = await club.GetFromJsonAsync<JsonElement>("/api/v1/me");
        var coachId = me.GetProperty("id").GetGuid();
        var mobile = SportApiFactory.NewMobile();
        var member = await club.PostAsJsonAsync("/api/v1/members", new { fullName = "نگار", mobile });
        var memberId = (await member.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var start = DateTimeOffset.UtcNow.AddDays(1);
        var session = await club.PostAsJsonAsync("/api/v1/sessions", new
        {
            title = "گروهی",
            kind = "group",
            startsAt = start,
            endsAt = start.AddHours(1),
            coachPersonId = coachId,
            memberIds = new[] { memberId },
        });
        var sessionId = (await session.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var (student, _) = await api.LoginAsync(mobile);
        Assert.Equal(HttpStatusCode.NoContent, (await student.PostAsync($"/api/v1/me/sessions/{sessionId}/cancel", null)).StatusCode);
        var still = await club.GetFromJsonAsync<JsonElement>($"/api/v1/sessions?from={Uri.EscapeDataString(start.AddHours(-1).ToString("O"))}&to={Uri.EscapeDataString(start.AddHours(2).ToString("O"))}");
        Assert.Contains(still.EnumerateArray(), s => s.GetProperty("id").GetGuid() == sessionId && s.GetProperty("status").GetString() == "published");

        var inbox = await club.GetFromJsonAsync<JsonElement>("/api/v1/inbox");
        Assert.Contains(inbox.EnumerateArray(), n => n.GetProperty("title").GetString() == "لغو جلسه");
    }
}
