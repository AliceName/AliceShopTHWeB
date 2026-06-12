using System.Linq;
using System.Threading.Tasks;
using AliceShop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AliceShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string status, string paymentStatus)
        {
            var ordersQuery = _context.Orders
                .Include(o => o.User)
                .AsQueryable();
            // 1. Loại bỏ khoảng trắng thừa nếu có
            string currentStatus = status?.Trim();

            // 2. Lọc theo tiến độ đơn hàng
            if (!string.IsNullOrEmpty(currentStatus) && !currentStatus.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                ordersQuery = ordersQuery.Where(o => o.Status == currentStatus);
            }

            // Lọc theo trạng thái dòng tiền (Thanh toán)
            if (!string.IsNullOrEmpty(paymentStatus) && paymentStatus != "All")
            {
                if (paymentStatus == "Unpaid")
                {
                    // Lọc thông minh: Lấy cả đơn ghi rõ 'Unpaid', đơn bị NULL hoặc đơn để trống cột dòng tiền
                    ordersQuery = ordersQuery.Where(o => o.PaymentStatus == "Unpaid"
                                                      || o.PaymentStatus == null
                                                      || o.PaymentStatus == "");
                }
                else
                {
                    ordersQuery = ordersQuery.Where(o => o.PaymentStatus == paymentStatus);
                }
            }

            // 2. 🔥 ĐIỂM QUAN TRỌNG: Bắt buộc phải là danh sách .ToListAsync()
            var orders = await ordersQuery.OrderByDescending(o => o.OrderDate).ToListAsync();

            ViewBag.CurrentStatus = status ?? "All";
            ViewBag.CurrentPaymentStatus = paymentStatus ?? "All";

            // Đẩy bảng map nhãn tiếng Việt ra ngoài giao diện cho Admin dễ đọc
            ViewBag.StatusLabels = GetStatusLabelsMapping();
            ViewBag.PaymentStatusLabels = GetPaymentStatusLabelsMapping();
            return View(orders);
        }

        // ── 2. DETAILS: XEM CHI TIẾT VÀ ĐƯA RA CẢ 2 NHÃN TRẠNG THÁI TIẾNG VIỆT ───────
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductSizeVariant)
                        .ThenInclude(psv => psv.Product)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductSizeVariant)
                        .ThenInclude(psv => psv.ProductSize)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();
            // Đóng gói nhãn thân thiện tiếng Việt để hiển thị lên thẻ Span ngoài UI
            ViewBag.StatusFriendlyName = GetFriendlyStatusName(order.Status);
            ViewBag.PaymentStatusFriendlyName = GetFriendlyPaymentStatusName(order.PaymentStatus);

            ViewBag.StatusLabels = GetStatusLabelsMapping();
            ViewBag.PaymentStatusLabels = GetPaymentStatusLabelsMapping();
            return View(order);
        }

        // ── 3. UPDATE ORDER STATUS: DUYỆT TIẾN ĐỘ CHẾ TÁC & HOÀN KHO KHI HỦY ──────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int id, string status)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductSizeVariant)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            // Nếu đơn hàng đã kết thúc (Thành công hoặc Hủy), khóa chặt không cho đổi linh tinh
            if (order.Status == "Completed" || order.Status == "Cancelled")
            {
                TempData["ErrorMessage"] = "Đơn hàng đã đóng biên dịch, không thể chỉnh sửa tiến độ xử lý hàng hóa.";
                return RedirectToAction(nameof(Details), new { id = order.Id });
            }

            // 🔥 LOGIC TỰ ĐỘNG HOÀN KHO: Nếu Admin bấm nút hủy đơn, cộng trả lại số lượng tồn kho cho từng Size
            if (status == "Cancelled" && order.Status != "Cancelled")
            {
                foreach (var detail in order.OrderDetails)
                {
                    if (detail.ProductSizeVariant != null)
                    {
                        detail.ProductSizeVariant.StockQuantity += detail.Quantity;
                        _context.ProductSizeVariants.Update(detail.ProductSizeVariant);
                    }
                }
            }

            order.Status = status;
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã cập nhật tiến độ xử lý đơn hàng thành: {GetFriendlyStatusName(status)}";
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }

        // ── 4. UPDATE PAYMENT STATUS: DUYỆT DÒNG TIỀN (XÁC NHẬN TIỀN VỀ KHI CHECK BANK) ─
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePaymentStatus(int id, string paymentStatus)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            // Cập nhật riêng lẻ trạng thái tiền tệ (Ví dụ: Tiền mặt COD đã thu, hoặc tiền VietQR đã ting ting)
            order.PaymentStatus = paymentStatus;
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã cập nhật trạng thái dòng tiền thanh toán thành: {GetFriendlyPaymentStatusName(paymentStatus)}";
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }

        // ── 💡 CÁC HÀM TRỢ LÝ (HELPER) ĐỂ ĐỒNG BỘ HIỂN THỊ TIẾNG VIỆT ──────────
        private string GetFriendlyStatusName(string status)
        {
            var mapping = GetStatusLabelsMapping();
            return mapping.ContainsKey(status) ? mapping[status] : status;
        }

        private string GetFriendlyPaymentStatusName(string paymentStatus)
        {
            var mapping = GetPaymentStatusLabelsMapping();
            return mapping.ContainsKey(paymentStatus) ? mapping[paymentStatus] : paymentStatus;
        }
        private Dictionary<string, string> GetStatusLabelsMapping()
        {
            return new Dictionary<string, string>
            {
                { "Pending", "Chờ phê duyệt đơn" },
                { "Processing", "Đng chế tác / Đóng gói" },
                { "Shipping", "Đang vận chuyển Premium" },
                { "Completed", "Giao thành công / Đã đóng" },
                { "Cancelled", "Đã hủy vận đơn" }
            };
        }

        private Dictionary<string, string> GetPaymentStatusLabelsMapping()
        {
            return new Dictionary<string, string>
            {
                { "Unpaid", "Chưa thanh toán (Chờ VietQR / Thu hộ COD)" },
                { "Paid", "Đã thanh toán thành công (Khớp lệnh)" },
                { "Refunded", "Đã hoàn trả tiền (Refunded)" }
            };
        }
    }
}
