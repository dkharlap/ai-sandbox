using Microsoft.EntityFrameworkCore;

namespace SimpleAccount.Preferences.Data;

public class PreferencesDbContext(DbContextOptions<PreferencesDbContext> options) : DbContext(options)
{
    public DbSet<CatalogModel> Catalog => Set<CatalogModel>();
    public DbSet<UserModelPreference> Preferences => Set<UserModelPreference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("prefs");

        modelBuilder.Entity<CatalogModel>(entity =>
        {
            entity.ToTable("model_catalog");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DisplayName).HasColumnName("display_name").IsRequired();
            entity.Property(e => e.Vendor).HasColumnName("vendor").IsRequired();
            entity.Property(e => e.IsActive).HasColumnName("is_active");

            entity.HasData(CatalogSeed.Models);
        });

        modelBuilder.Entity<UserModelPreference>(entity =>
        {
            entity.ToTable("user_model_preferences");
            entity.HasKey(e => new { e.UserId, e.ModelId });
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ModelId).HasColumnName("model_id");
            entity.Property(e => e.Rank).HasColumnName("rank");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasOne(e => e.Model)
                .WithMany()
                .HasForeignKey(e => e.ModelId)
                .OnDelete(DeleteBehavior.Restrict);

            // Rank is unique per user. Deferred so a whole-list replace can renumber within
            // one transaction without tripping the constraint mid-update.
            entity.HasIndex(e => new { e.UserId, e.Rank })
                .IsUnique()
                .HasDatabaseName("ux_user_model_preferences_user_rank");
        });
    }
}
