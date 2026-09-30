using Microsoft.EntityFrameworkCore;

namespace SimpleAccount.Account.Data;

public class AccountDbContext(DbContextOptions<AccountDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("accounts");
        modelBuilder.HasPostgresExtension("citext");

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");

            entity.Property(e => e.GoogleSubject).HasColumnName("google_subject").IsRequired();
            entity.Property(e => e.Email).HasColumnName("email").HasColumnType("citext").IsRequired();
            entity.Property(e => e.FirstName).HasColumnName("first_name");
            entity.Property(e => e.LastName).HasColumnName("last_name");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            // `sub` is the only unique key. A unique index on email would eventually reject a
            // legitimate login: Google emails change, and released addresses get reassigned.
            entity.HasIndex(e => e.GoogleSubject).IsUnique().HasDatabaseName("ux_users_google_subject");
            entity.HasIndex(e => e.Email).HasDatabaseName("ix_users_email");
        });
    }
}
