using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AliceShop.Models
{
    public class Material
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Column(TypeName = "nvarchar(50)")]   // Hỗ trợ tiếng Việt
        public string Name { get; set; } = string.Empty;

        // Thuộc tính liên kết ngược (Navigation Property) đến danh sách sản phẩm
        public List<Product>? Products { get; set; }
    }
}
