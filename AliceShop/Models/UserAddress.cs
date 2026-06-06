using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AliceShop.Models
{
    public class UserAddress
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên người nhận")]
        [StringLength(100)]
        [Display(Name = "Họ tên người nhận")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [StringLength(15)]
        [DataType(DataType.PhoneNumber)]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập chi tiết địa chỉ giao hàng")]
        [StringLength(500)]
        [Display(Name = "Địa chỉ chi tiết")]
        public string DetailedAddress { get; set; }

        [Display(Name = "Đặt làm địa chỉ mặc định")]
        public bool IsDefault { get; set; }
    }
}
