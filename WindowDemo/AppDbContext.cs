using Microsoft.EntityFrameworkCore;

namespace WindowDemo
{
    public class AppDbContext : DbContext
    {
        public DbSet<Customer> Customers { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderLine> OrderLines { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!; // NYTT

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Order>()
                .HasMany(o => o.Items)
                .WithOne(ol => ol.Order)
                .HasForeignKey(ol => ol.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // NYTT: Product -> Category relation
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderLine>()
                .Property(ol => ol.UnitPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(o => o.Subtotal)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(o => o.Shipping)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(o => o.Vat)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(o => o.Total)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Customer>().Property(c => c.Email).HasMaxLength(256);
            modelBuilder.Entity<Customer>().Property(c => c.Name).HasMaxLength(200);

            modelBuilder.Entity<Product>().Property(p => p.Name).HasMaxLength(200);
            modelBuilder.Entity<Product>().Property(p => p.Description).HasMaxLength(1000);
            modelBuilder.Entity<Product>().Ignore(p => p.CategoryName);
            modelBuilder.Entity<Product>().Ignore(p => p.CategoryEnum);

            modelBuilder.Entity<Category>().Property(c => c.Name).HasMaxLength(100);

            modelBuilder.Entity<Order>().Property(o => o.ShippingType).HasMaxLength(50);
            modelBuilder.Entity<Order>().Property(o => o.RecipientName).HasMaxLength(200);
            modelBuilder.Entity<Order>().Property(o => o.Address).HasMaxLength(300);
            modelBuilder.Entity<Order>().Property(o => o.Postal).HasMaxLength(20);
            modelBuilder.Entity<Order>().Property(o => o.City).HasMaxLength(100);

            modelBuilder.Entity<OrderLine>().Property(ol => ol.ProductName).HasMaxLength(200);

            modelBuilder.Entity<Customer>().HasIndex(c => c.Email);
            modelBuilder.Entity<OrderLine>().HasIndex(ol => ol.ProductId);
            modelBuilder.Entity<Order>().HasIndex(o => o.Date);
            modelBuilder.Entity<Category>().HasIndex(c => c.Name).IsUnique();

            // Seed kategorier
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Tröjor" },
                new Category { Id = 2, Name = "Byxor" },
                new Category { Id = 3, Name = "Skor" }
            );
        }
    }
}