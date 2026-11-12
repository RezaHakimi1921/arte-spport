using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Arte.Sport.Api.Sms;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Arte.Sport.Tests;

public class SportApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PostgreSqlContainer? _pg;
    private string _connectionString = Environment.GetEnvironmentVariable("SPORT_TEST_DB") ?? "";

    public async Task InitializeAsync()
    {
        if (string.IsNullOrEmpty(_connectionString))
        {
            _pg = new PostgreSqlBuilder("postgres:17-alpine").Build();
            await _pg.StartAsync();
            _connectionString = _pg.GetConnectionString();
        }
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        if (_pg is not null) await _pg.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", _connectionString);
        builder.UseSetting("Database:MigrateOnStartup", "true");
        builder.UseSetting("Jwt:Key", "test-only-sport-key-not-for-production-32");
        builder.UseSetting("Jwt:Issuer", "arte-spport");
        builder.UseSetting("Jwt:Audience", "arte-spport");
        builder.UseSetting("Otp:Pepper", "test-pepper-test-pepper-test-pepper");
    }

    public FakeSmsSender Sms => Services.GetRequiredService<FakeSmsSender>();

    public HttpClient Client() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        HandleCookies = true,
    });

    private static int _mobileSeq = 2_000_000;
    public static string NewMobile() => $"0913{Interlocked.Increment(ref _mobileSeq):D7}";

    public async Task<(HttpClient Client, JsonElement Session)> LoginAsync(string? mobile = null)
    {
        mobile ??= NewMobile();
        var client = Client();
        var otp = await client.PostAsJsonAsync("/api/v1/auth/otp/request", new { mobile });
        if (!otp.IsSuccessStatusCode)
            throw new HttpRequestException(await otp.Content.ReadAsStringAsync(), null, otp.StatusCode);
        var code = Sms.Sent[mobile];
        var res = await client.PostAsJsonAsync("/api/v1/auth/otp/verify", new { mobile, code });
        res.EnsureSuccessStatusCode();
        var session = await res.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.GetProperty("accessToken").GetString());
        return (client, session);
    }

    public async Task<(HttpClient Client, Guid BranchId)> NewClubAsync(string? mobile = null)
    {
        var (client, _) = await LoginAsync(mobile);
        var res = await client.PostAsJsonAsync("/api/v1/brands", new
        {
            name = "باشگاه تست",
            branchName = "شعبه مرکزی",
            ownerName = "مدیر تست",
        });
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
        return (client, body.GetProperty("branchId").GetGuid());
    }

    public static void UseToken(HttpClient client, JsonElement session) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.GetProperty("accessToken").GetString());
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<SportApiFactory>
{
    public const string Name = "api";
}
