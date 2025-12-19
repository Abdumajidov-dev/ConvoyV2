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
    }
}
