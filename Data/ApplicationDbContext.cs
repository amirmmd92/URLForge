using Distributed_URL_Shortener_with_Analytics.Models;
using Microsoft.EntityFrameworkCore;

namespace Distributed_URL_Shortener_with_Analytics.Data
{
    // Database context for the application
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<ShortLink> ShortLinks { get; set; }
        public DbSet<ClickAnalytics> ClickAnalytics { get; set; }
        public DbSet<ApiKey> ApiKeys { get; set; }
        public DbSet<AppSettings> AppSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure indexes for better performance
            modelBuilder.Entity<ShortLink>()
                .HasIndex(s => s.ShortCode)
                .IsUnique();

            modelBuilder.Entity<ShortLink>()
                .HasIndex(s => s.UserId);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<ApiKey>()
                .HasIndex(a => a.Key)
                .IsUnique();

            modelBuilder.Entity<ClickAnalytics>()
                .HasIndex(c => c.ShortLinkId);

            modelBuilder.Entity<ClickAnalytics>()
                .HasIndex(c => c.ClickedAt);
        }
    }
}

