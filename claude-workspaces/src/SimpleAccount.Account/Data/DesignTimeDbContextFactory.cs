using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SimpleAccount.Account.Data;

/// <summary>
/// Used only by `dotnet ef` at design time. At runtime the connection string comes from
/// Aspire via AddNpgsqlDbContext, so this never participates in a real request.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AccountDbContext>
{
    public AccountDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AccountDbContext>()
            .UseNpgsql("Host=localhost;Database=accountsdb;Username=postgres")
            .Options;
        return new AccountDbContext(options);
    }
}
