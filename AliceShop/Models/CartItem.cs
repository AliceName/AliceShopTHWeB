using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AliceShop.Models
{
    public class CartItem
    {
        [Key]
        public int Id { get; set; }

        // 1. Khóa ngoại liên kết tới tài khoản người dùng hệ thống Identity
        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        // 2. Khóa ngoại liên kết trực tiếp tới mã biến thể kích cỡ (Size) của sản phẩm
        [Required]
        public int ProductSizeVariantId { get; set; }

        [ForeignKey("ProductSizeVariantId")]
        public virtual ProductSizeVariant ProductSizeVariant { get; set; }

        // 3. Số lượng sản phẩm khách mong muốn bỏ vào giỏ hàng
        [Required(ErrorMessage = "Số lượng mua không được để trống.")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng đặt mua tối thiểu phải từ 1 sản phẩm trở lên.")]
        public int Quantity { get; set; }

        // 4. Ngày giờ thêm sản phẩm vào giỏ để hỗ trợ phân tích xu hướng mua sắm hoặc dọn dẹp giỏ hàng định kỳ
        [Required]
        public DateTime DateCreated { get; set; } = DateTime.Now;
    }
}
