using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace AliceShop.Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext>options) : base(options)
        {
        }
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Material> Materials { get; set; }
        public DbSet<ProductSize> ProductSizes { get; set; }
        public DbSet<ProductSizeVariant> ProductSizeVariants { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<UserAddress> UserAddresses { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // 1. Nhánh 1: Từ Category sang ProductSizes (Tắt xóa tự động để tránh cycle path)
            builder.Entity<ProductSize>()
                .HasOne(ps => ps.Category)
                .WithMany()
                .HasForeignKey(ps => ps.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // 2. Nhánh 2: Từ ProductSizes sang bảng trung gian ProductSizeVariants (Tắt xóa tự động)
            builder.Entity<ProductSizeVariant>()
                .HasOne(pv => pv.ProductSize)
                .WithMany(ps => ps.ProductSizeVariants)
                .HasForeignKey(pv => pv.ProductSizeId)
                .OnDelete(DeleteBehavior.Restrict);

            // 3. Khắc phục cảnh báo: "No store type was specified for the decimal property 'Price'"
            builder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            builder.Entity<ProductSizeVariant>()
                .Property(pv => pv.Price)
                .HasColumnType("decimal(18,2)");
        }
    }
}
