using Arte.Sport.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Arte.Sport.Api.Data;

public sealed class SportDbContextFactory : IDesignTimeDbContextFactory<SportDbContext>
{
    public SportDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("SPORT_CONNECTION")
            ?? "Host=localhost;Database=arte_spport;Username=postgres;Password=postgres";
        var options = new DbContextOptionsBuilder<SportDbContext>().UseNpgsql(cs).Options;
        return new SportDbContext(options);
    }
}
