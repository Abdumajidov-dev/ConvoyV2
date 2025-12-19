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
    }
}
