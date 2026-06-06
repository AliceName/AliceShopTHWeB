namespace AliceShop.Models
{
    public class ShoppingCart
    {
        // Tập hợp danh sách các dòng mặt hàng đang nằm trong giỏ
        public List<CartItem> Items { get; set; } = new List<CartItem>();

        // 1. TỰ ĐỘNG TÍNH TOÁN: Tổng số lượng trang sức (món) đang có trong giỏ hàng
        public int TotalQuantity => Items.Sum(item => item.Quantity);

        // 2. TỰ ĐỘNG TÍNH TOÁN: Tổng thành tiền của toàn bộ giỏ hàng (tính theo giá riêng của từng Size)
        public decimal TotalAmount => Items.Sum(item => item.ProductSizeVariant.Price * item.Quantity);

        // 3. TỰ ĐỘNG TÍNH TOÁN: Kiểm tra nhanh trạng thái giỏ hàng trống
        public bool IsEmpty => !Items.Any();

        // ── CÁC PHƯƠNG THỨC XỬ LÝ LOGIC DỰA THEO BIẾN THỂ SIZE (SIZE VARIANT) ──

        // 4. Hàm thêm mới hoặc cộng dồn số lượng món vào giỏ hàng
        public void AddItem(CartItem item)
        {
            // 🔥 ĐA SỬA: Lọc tìm kiếm phần tử trùng lặp dựa theo ProductSizeVariantId thay vì ProductId
            var existingItem = Items.FirstOrDefault(i => i.ProductSizeVariantId == item.ProductSizeVariantId);

            if (existingItem != null)
            {
                // Nếu cùng một sản phẩm và cùng một kích cỡ -> Tiến hành cộng dồn số lượng mua
                existingItem.Quantity += item.Quantity;
            }
            else
            {
                // Nếu là size mới hoàn toàn -> Nạp thêm một dòng mới vào danh sách
                Items.Add(item);
            }
        }

        // 5. Hàm loại bỏ hoàn toàn một dòng mặt hàng ra khỏi giỏ
        public void RemoveItem(int sizeVariantId)
        {
            // 🔥 ĐA SỬA: Xóa bỏ dựa theo mã biến thể kích cỡ cụ thể được chọn
            Items.RemoveAll(i => i.ProductSizeVariantId == sizeVariantId);
        }

        // 6. Hàm cập nhật đè số lượng (Phục vụ cho luồng nhập số lượng trực tiếp tại trang giỏ hàng)
        public void UpdateQuantity(int sizeVariantId, int quantity)
        {
            var targetItem = Items.FirstOrDefault(i => i.ProductSizeVariantId == sizeVariantId);
            if (targetItem != null && quantity > 0)
            {
                targetItem.Quantity = quantity;
            }
        }

        // ── CÁC HÀM KHỞI TẠO CONSTRUCTORS ──
        public ShoppingCart()
        {
        }

        public ShoppingCart(List<CartItem> items)
        {
            Items = items ?? new List<CartItem>();
        }
    }
}
