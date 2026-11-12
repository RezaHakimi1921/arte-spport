using System.Text;
using Arte.Sport.Api.Auth;
using Arte.Sport.Api.Clubs;
using Arte.Sport.Api.Members;
using Arte.Sport.Api.Schedule;
using Arte.Sport.Api.Security;
using Arte.Sport.Api.Sms;
using Arte.Sport.Core.Branching;
using Arte.Sport.Core.Data;
using Arte.Sport.Core.Features;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

var jwtKey = config["Jwt:Key"] ?? "";
var pepper = config["Otp:Pepper"] ?? "";
if (Encoding.UTF8.GetByteCount(jwtKey) < 32 || pepper.Length < 16)
    throw new InvalidOperationException("Jwt:Key and Otp:Pepper must be configured.");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.Configure<FeatureOptions>(config.GetSection(FeatureOptions.Section));
builder.Services.Configure<AvalAiOptions>(config.GetSection(AvalAiOptions.Section));
builder.Services.AddScoped<RequestUser>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<OtpService>();

if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddSingleton<FakeSmsSender>();
    builder.Services.AddSingleton<ISmsSender>(sp => sp.GetRequiredService<FakeSmsSender>());
}
else if (config["Sms:Provider"] == "iranpayamak")
{
    builder.Services.AddHttpClient<ISmsSender, IranPayamakOtpSender>(c =>
        c.BaseAddress = new Uri("https://api.iranpayamak.com/"));
}
else
{
    builder.Services.AddSingleton<ISmsSender, UnavailableSmsSender>();
}

builder.Services.AddDbContext<SportDbContext>(o =>
    o.UseNpgsql(config.GetConnectionString("Default")));

var jwt = config.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? new JwtOptions();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = TokenService.SigningKey(jwtKey),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = SportClaims.PersonId,
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseExceptionHandler(handler => handler.Run(async http =>
{
    var error = http.Features.Get<IExceptionHandlerFeature>()?.Error;
    var logger = http.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Errors");
    if (error is BranchViolationException)
    {
        http.Response.StatusCode = StatusCodes.Status404NotFound;
        await http.Response.WriteAsJsonAsync(new { title = "یافت نشد." });
        return;
    }
    logger.LogError(error, "Unhandled error");
    http.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await http.Response.WriteAsJsonAsync(new { title = "خطای داخلی." });
}));

app.UseAuthentication();
app.UseMiddleware<BranchResolutionMiddleware>();
app.UseAuthorization();

if (config.GetValue("Database:MigrateOnStartup", true))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<SportDbContext>().Database.Migrate();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/health/ready", async (SportDbContext db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct)
        ? Results.Ok(new { status = "ready" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));

app.MapGet("/api/v1/features", (Microsoft.Extensions.Options.IOptions<FeatureOptions> features, Microsoft.Extensions.Options.IOptions<AvalAiOptions> ai) =>
    Results.Ok(new
    {
        receipts = features.Value.Receipts,
        trainingPlans = features.Value.TrainingPlans,
        avalAi = features.Value.AvalAi,
        avalAiCanCall = AvalAiGate.CanCall(features.Value, ai.Value),
        avalAiRetentionDays = ai.Value.RetentionDays,
        avalAiMonthlyCostCapRials = ai.Value.MonthlyCostCapRials,
    }));

AuthEndpoints.Map(app);
ClubEndpoints.Map(app);
MemberEndpoints.Map(app);
ScheduleEndpoints.Map(app);

if (args.Length > 0 && args[0] == "set-admin")
    Environment.Exit(await Arte.Sport.Api.Auth.SetAdminCommand.RunAsync(app.Services, args));

app.Run();

public partial class Program;
