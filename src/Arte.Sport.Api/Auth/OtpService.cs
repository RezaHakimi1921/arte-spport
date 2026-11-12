using System.Security.Cryptography;
using System.Text;
using Arte.Sport.Api.Sms;
using Arte.Sport.Core.Data;
using Arte.Sport.Core.Identity;
using Microsoft.EntityFrameworkCore;

namespace Arte.Sport.Api.Auth;

public sealed class OtpOptions
{
    public const string Section = "Otp";
    public string Pepper { get; set; } = "";
    public int LifetimeSeconds { get; set; } = 120;
    public int Length { get; set; } = 5;
    public int MaxAttempts { get; set; } = 5;
}

public sealed class OtpService(SportDbContext db, ISmsSender sms, IConfiguration config, TimeProvider clock)
{
    public async Task RequestAsync(string mobile, CancellationToken ct)
    {
        var otp = config.GetSection(OtpOptions.Section).Get<OtpOptions>() ?? new OtpOptions();
        var code = NewCode(otp.Length);
        var now = clock.GetUtcNow();
        var old = await db.OtpChallenges.Where(x => x.Mobile == mobile).ToListAsync(ct);
        db.OtpChallenges.RemoveRange(old);
        db.OtpChallenges.Add(new OtpChallenge
        {
            Id = Guid.NewGuid(),
            Mobile = mobile,
            CodeHash = Hash(otp.Pepper, mobile, code),
            ExpiresAt = now.AddSeconds(otp.LifetimeSeconds <= 0 ? 120 : otp.LifetimeSeconds),
            CreatedAt = now,
        });
        await db.SaveChangesAsync(ct);
        await sms.SendOtpAsync(mobile, code, ct);
    }

    public async Task<bool> VerifyAsync(string mobile, string code, CancellationToken ct)
    {
        var otp = config.GetSection(OtpOptions.Section).Get<OtpOptions>() ?? new OtpOptions();
        var row = await db.OtpChallenges.Where(x => x.Mobile == mobile).OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(ct);
        if (row is null) return false;
        var now = clock.GetUtcNow();
        if (row.ExpiresAt < now || row.Attempts >= otp.MaxAttempts)
        {
            db.OtpChallenges.Remove(row);
            await db.SaveChangesAsync(ct);
            return false;
        }

        var ok = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(row.CodeHash),
            Encoding.UTF8.GetBytes(Hash(otp.Pepper, mobile, code)));
        if (!ok)
        {
            row.Attempts++;
            await db.SaveChangesAsync(ct);
            return false;
        }

        db.OtpChallenges.Remove(row);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public static string Hash(string pepper, string mobile, string code)
    {
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(pepper), Encoding.UTF8.GetBytes($"{mobile}:{code}"));
        return Convert.ToHexString(hash);
    }

    private static string NewCode(int length)
    {
        length = length is < 4 or > 8 ? 5 : length;
        var n = RandomNumberGenerator.GetInt32(0, (int)Math.Pow(10, length));
        return n.ToString().PadLeft(length, '0');
    }
}
