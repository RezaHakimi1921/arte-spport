using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Arte.Sport.Api.Security;
using Arte.Sport.Core.Clubs;
using Microsoft.IdentityModel.Tokens;

namespace Arte.Sport.Api.Auth;

public sealed class JwtOptions
{
    public const string Section = "Jwt";
    public string Issuer { get; set; } = "arte-spport";
    public string Audience { get; set; } = "arte-spport";
    public string Key { get; set; } = "";
    public int AccessHours { get; set; } = 12;
}

public sealed class TokenService(IConfiguration config, TimeProvider clock)
{
    public string Issue(Guid personId, Membership? membership)
    {
        var jwt = config.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? new JwtOptions();
        var hours = jwt.AccessHours <= 0 ? 12 : jwt.AccessHours;
        var claims = new List<Claim> { new(SportClaims.PersonId, personId.ToString()) };
        if (membership is not null)
        {
            claims.Add(new Claim(SportClaims.BranchId, membership.BranchId.ToString()));
            claims.Add(new Claim(SportClaims.MembershipId, membership.Id.ToString()));
        }

        var token = new JwtSecurityToken(
            issuer: jwt.Issuer,
            audience: jwt.Audience,
            claims: claims,
            notBefore: clock.GetUtcNow().UtcDateTime,
            expires: clock.GetUtcNow().UtcDateTime.AddHours(hours),
            signingCredentials: new SigningCredentials(SigningKey(jwt.Key), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static SymmetricSecurityKey SigningKey(string key) => new(Encoding.UTF8.GetBytes(key));
}
