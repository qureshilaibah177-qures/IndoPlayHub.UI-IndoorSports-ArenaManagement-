using IndoPlayHub.UI.Models;
using Microsoft.EntityFrameworkCore;

namespace IndoPlayHub.UI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // =========================================================
        // ARENAS
        // =========================================================

        public DbSet<Arena> Arenas { get; set; }
        public DbSet<Customer> Customers { get; set; }

        // =========================================================
        // SPORTS SYSTEM
        // =========================================================

        public DbSet<Category> Categories { get; set; }

        public DbSet<CourtOrMenuItem> CourtsAndMenuItems { get; set; }

        // =========================================================
        // MENU SYSTEM
        // =========================================================

        public DbSet<MenuCategory> MenuCategories { get; set; }

        public DbSet<ArenaMenuItem> MenuItems { get; set; }

        // =========================================================
        // BOOKINGS
        // =========================================================

        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Notification> Notifications { get; set; }



        // =========================================================
        // MODEL CONFIGURATION
        // =========================================================

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            // =====================================================
            // BOOKING → ARENA
            // =====================================================

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Arena)
                .WithMany(a => a.Bookings)
                .HasForeignKey(b => b.ArenaID)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // BOOKING → COURT / SPORTS ITEM
            // =====================================================

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.CourtOrMenuItem)
                .WithMany(c => c.Bookings)
                .HasForeignKey(b => b.ItemID)
                .HasPrincipalKey(c => c.ItemID)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // COURT / SPORTS ITEM → SPORTS CATEGORY
            // =====================================================

            modelBuilder.Entity<CourtOrMenuItem>()
                .HasOne(c => c.Category)
                .WithMany(category => category.Items)
                .HasForeignKey(c => c.CategoryID)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // COURT / SPORTS ITEM → ARENA
            // =====================================================

            modelBuilder.Entity<CourtOrMenuItem>()
                .HasOne(c => c.Arena)
                .WithMany(a => a.Items)
                .HasForeignKey(c => c.ArenaID)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // MENU ITEM → ARENA
            // =====================================================

            modelBuilder.Entity<ArenaMenuItem>()
                .HasOne(m => m.Arena)
                .WithMany()
                .HasForeignKey(m => m.ArenaID)
                .OnDelete(DeleteBehavior.Cascade);


            // =====================================================
            // MENU ITEM → MENU CATEGORY
            // =====================================================

            modelBuilder.Entity<ArenaMenuItem>()
                .HasOne(m => m.MenuCategory)
                .WithMany(c => c.MenuItems)
                .HasForeignKey(m => m.MenuCategoryID)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // MENU CATEGORY
            // =====================================================

            modelBuilder.Entity<MenuCategory>()
                .HasKey(c => c.MenuCategoryID);

            modelBuilder.Entity<MenuCategory>()
                .Property(c => c.MenuCategoryName)
                .IsRequired()
                .HasMaxLength(50);


            // =====================================================
            // ARENA MENU ITEM
            // =====================================================

            modelBuilder.Entity<ArenaMenuItem>()
                .HasKey(m => m.MenuItemID);

            modelBuilder.Entity<ArenaMenuItem>()
                .Property(m => m.ItemName)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<ArenaMenuItem>()
                .Property(m => m.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<ArenaMenuItem>()
                .Property(m => m.ImageUrl)
                .HasMaxLength(255);

            // =====================================================
            // NOTIFICATION → CUSTOMER
            // =====================================================

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Customer)
                .WithMany()
                .HasForeignKey(n => n.CustomerID)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}