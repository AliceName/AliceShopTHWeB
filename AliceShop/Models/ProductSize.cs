using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AliceShop.Models
{
    [Table("ProductSizes")]
    public class ProductSize
    {
        
        [Key]
        public int Id { get; set; }

        [StringLength(100, ErrorMessage = "Tên kích cỡ không được vượt quá 100 ký tự.")]
        [Display(Name = "Kích cỡ (Size)")]
        public string SizeName { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại danh mục áp dụng.")]
        public int CategoryId { get; set; }
        // ── QUAN HỆ ĐIỀU HƯỚNG (NAVIGATION PROPERTIES) ──
        // Một kích cỡ danh mục sẽ xuất hiện trong nhiều biến thể của các sản phẩm khác nhau
        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }
        public virtual ICollection<ProductSizeVariant> ProductSizeVariants { get; set; } = new List<ProductSizeVariant>();
    }
}
