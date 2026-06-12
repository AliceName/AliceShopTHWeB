using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AliceShop.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        // Khóa ngoại liên kết với Identity User (Có thể Null nếu cho phép khách vãng lai đặt hàng)
        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [Required]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        // ── PHÂN KHU THÔNG TIN GIAO HÀNG (SẼ ĐỒNG BỘ VỚI SỔ ĐỊA CHỈ SAU NÀY) ──
        [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận.")]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại giao hàng.")]
        [StringLength(15)]
        [DataType(DataType.PhoneNumber)]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ nhận hàng chi tiết.")]
        [StringLength(500)]
        public string ShippingAddress { get; set; }

        [StringLength(500)]
        public string? OrderNote { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Processing, Shipped, Completed, Cancelled
        // phương thức thanh toán
        public string PaymentMethod { get; set; } = "BankTransfer";

        //Trạng thái thanh toán 
        public string PaymentStatus { get; set; } = "Unpaid";

        // Quan hệ 1-N sang bảng chi tiết đơn hàng
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
