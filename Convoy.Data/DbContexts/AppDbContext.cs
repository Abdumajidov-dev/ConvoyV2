using Convoy.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Convoy.Data.DbContexts;

public class AppDbConText : DbContext
{
    public AppDbConText(DbContextOptions<AppDbConText> options) : base(options)
    {

    }

    // DbSets - faqat EF Core bilan ishlaydigan entity'lar
    public DbSet<User> Users { get; set; }
    public DbSet<OtpCode> OtpCodes { get; set; }
    public DbSet<TokenBlacklist> TokenBlacklists { get; set; }
    public DbSet<UserStatusReport> userStatusReports { get; set; }

    // Location uchun DbSet YO'Q - u Dapper bilan ishlaydi

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User entity configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Phone).HasMaxLength(20);
        });

        // OtpCode entity configuration
        modelBuilder.Entity<OtpCode>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(6);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.ExpiresAt).IsRequired();
            entity.Property(e => e.IsUsed).IsRequired().HasDefaultValue(false);

            // Index for faster phone number lookup
            entity.HasIndex(e => e.PhoneNumber);
            entity.HasIndex(e => e.ExpiresAt);
        });

        // TokenBlacklist entity configuration
        modelBuilder.Entity<TokenBlacklist>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TokenJti).IsRequired().HasMaxLength(100);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.BlacklistedAt).IsRequired();
            entity.Property(e => e.ExpiresAt).IsRequired();
            entity.Property(e => e.Reason).HasMaxLength(50);

            // Unique constraint for token JTI
            entity.HasIndex(e => e.TokenJti).IsUnique();

            // Index for faster user lookup
            entity.HasIndex(e => e.UserId);

            // Index for cleanup expired tokens
            entity.HasIndex(e => e.ExpiresAt);

            // Foreign key relationship
            entity.HasOne(tb => tb.User)
                  .WithMany()
                  .HasForeignKey(tb => tb.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
