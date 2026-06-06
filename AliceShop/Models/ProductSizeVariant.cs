using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AliceShop.Models
{
    [Table("ProductSizeVariants")]
    public class ProductSizeVariant
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int ProductSizeId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá bán cho kích cỡ này.")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Giá bán theo Size")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng tồn kho cho kích cỡ này.")]
        [Display(Name = "Số lượng tồn kho")]
        public int StockQuantity { get; set; }

        // ── QUAN HỆ ĐIỀU HƯỚNG BẢNG (NAVIGATION PROPERTIES) ──

        // Liên kết ngược về bảng sản phẩm gốc
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        // Liên kết về bảng kích cỡ danh mục
        [ForeignKey("ProductSizeId")]
        public virtual ProductSize ProductSize { get; set; }
    }
}
