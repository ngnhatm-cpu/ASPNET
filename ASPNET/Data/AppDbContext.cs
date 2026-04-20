using Microsoft.EntityFrameworkCore;
using ASPNET.Models;

namespace ASPNET.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Các bảng trong database
    public DbSet<Category> Categories { get; set; }
    public DbSet<Manga> Mangas { get; set; }
    public DbSet<Chapter> Chapters { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<UserLibrary> UserLibraries { get; set; }
    public DbSet<TopupTransaction> TopupTransactions { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Rating> Ratings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Category → Mangas (1 - nhiều)
        modelBuilder.Entity<Manga>()
            .HasOne(m => m.Category)
            .WithMany(c => c.Mangas)
            .HasForeignKey(m => m.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Manga → Chapters (1 - nhiều)
        modelBuilder.Entity<Chapter>()
            .HasOne(c => c.Manga)
            .WithMany(m => m.Chapters)
            .HasForeignKey(c => c.MangaId)
            .OnDelete(DeleteBehavior.Cascade);

        // User → Orders (1 - nhiều)
        modelBuilder.Entity<Order>()
            .HasOne(o => o.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Order → OrderItems (1 - nhiều)
        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Order)
            .WithMany(o => o.OrderItems)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Chapter → OrderItems (1 - nhiều)
        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Chapter)
            .WithMany(c => c.OrderItems)
            .HasForeignKey(oi => oi.ChapterId)
            .OnDelete(DeleteBehavior.Restrict);

        // UserLibrary mapping
        modelBuilder.Entity<UserLibrary>()
            .HasOne(ul => ul.User)
            .WithMany(u => u.Library)
            .HasForeignKey(ul => ul.UserId);

        modelBuilder.Entity<UserLibrary>()
            .HasOne(ul => ul.Chapter)
            .WithMany(c => c.UserLibraries)
            .HasForeignKey(ul => ul.ChapterId);

        // User → TopupTransactions (1 - nhiều)
        modelBuilder.Entity<TopupTransaction>()
            .HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // User → Comments (1 - nhiều)
        modelBuilder.Entity<Comment>()
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Chapter → Comments (1 - nhiều)
        modelBuilder.Entity<Comment>()
            .HasOne(c => c.Chapter)
            .WithMany()
            .HasForeignKey(c => c.ChapterId)
            .OnDelete(DeleteBehavior.Cascade);

        // User → Ratings (1 - nhiều)
        modelBuilder.Entity<Rating>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Manga → Ratings (1 - nhiều)
        modelBuilder.Entity<Rating>()
            .HasOne(r => r.Manga)
            .WithMany()
            .HasForeignKey(r => r.MangaId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique: Username, Email
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();
    }
}
