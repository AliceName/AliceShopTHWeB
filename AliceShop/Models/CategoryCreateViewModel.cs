using System.ComponentModel.DataAnnotations;

namespace AliceShop.Models
{
    public class CategoryCreateViewModel
    {
        [Required(ErrorMessage = "Tên danh mục không được để trống.")]
        [StringLength(100)]
        public string Name { get; set; }

        // Danh sách chuỗi ký tự chứa các tên size được tạo động từ giao diện Form
        public List<string> CategorySizes { get; set; } = new List<string>();
    }
}
