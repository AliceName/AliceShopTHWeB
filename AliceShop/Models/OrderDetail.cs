using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AliceShop.Models
{
    public class OrderDetail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }

        [Required]
        public int ProductSizeVariantId { get; set; }

        [ForeignKey("ProductSizeVariantId")]
        public virtual ProductSizeVariant ProductSizeVariant { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; } // Giá chốt đóng băng lịch sử để làm báo cáo doanh thu
    }
}
