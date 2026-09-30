using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SimpleAccount.Preferences.Data;

/// <summary>Design-time only; runtime connection strings come from Aspire.</summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PreferencesDbContext>
{
    public PreferencesDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PreferencesDbContext>()
            .UseNpgsql("Host=localhost;Database=prefsdb;Username=postgres")
            .Options;
        return new PreferencesDbContext(options);
    }
}
