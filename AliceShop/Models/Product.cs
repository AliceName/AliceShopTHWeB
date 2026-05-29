using System.ComponentModel.DataAnnotations;

namespace AliceShop.Models
{
    public class Product
    {
        public int Id { get; set; }
        [Required, StringLength(100)]
        public string Name { get; set; }
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }
        public string Description { get; set; }
        public string? ImageUrl { get; set; }
        public List<ProductImage>? Images { get; set; }
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        // ════ THÊM MỚI: LIÊN KẾT BẢNG CHẤT LIỆU ════
        [Required]
        public int MaterialId { get; set; }
        public Material? Material { get; set; }
    }
}
