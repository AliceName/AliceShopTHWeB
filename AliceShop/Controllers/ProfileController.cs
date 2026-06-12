using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AliceShop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AliceShop.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ── 1. INDEX: QUẢN LÝ SỔ ĐỊA CHỈ GIAO HÀNG KHÁCH HÀNG ─────────────────────────
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var addresses = await _context.UserAddresses
                .Where(a => a.UserId == user.Id)
                .OrderByDescending(a => a.IsDefault)
                .ToListAsync();

            return View("Index", addresses);
        }

        [HttpGet]
        public IActionResult CreateAddress()
        {
            // Khởi tạo một thực thể địa chỉ trống hoàn toàn mới
            var newAddress = new UserAddress();

            // Truyền trực tiếp đối tượng đơn lẻ này vào đúng file View "Create"
            return View("Create", newAddress);
        }

        // ── 2. 🔥 ĐỒNG BỘ URL ĐƠN HÀNG: MAP ĐỊNH TUYẾN TRÌNH DUYỆT VỀ /Profile/Orders ──
        [HttpGet]
        [ActionName("Orders")] // Ép trình duyệt nhận diện URL /Profile/Orders cũ để tránh lỗi 404
        public async Task<IActionResult> History()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Nạp thông tin đơn hàng bám sát tài khoản Client phục vụ bảng thống kê VIP
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductSizeVariant)
                        .ThenInclude(psv => psv.Product)
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View("History", orders); // Chỉ định đích danh tệp Views/Profile/History.cshtml để render
        }

        // ── 3. 🔥 XEM CHI TIẾT ĐƠN HÀNG DÀNH CHO KHÁCH (CUSTOMERDETAILS) ──────────────
        [HttpGet]
        public async Task<IActionResult> CustomerDetails(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Nạp sâu đa tầng liên kết bảng để thắp sáng sơ đồ Timeline và tóm tắt tiền tệ
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductSizeVariant)
                        .ThenInclude(psv => psv.Product)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductSizeVariant)
                        .ThenInclude(psv => psv.ProductSize)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == user.Id); // Chốt chặn bảo mật chống tấn công IDOR

            if (order == null) return NotFound("Yêu cầu vận đơn không tồn tại hoặc không thuộc quyền sở hữu.");

            return View(order); // Trả về Views/Profile/CustomerDetails.cshtml
        }

        // ── 4. SỔ ĐỊA CHỈ: THÊM ĐỊA CHỈ MỚI (POST) ───────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAddress(UserAddress address)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Thiết lập thông tin khóa ngoại và dọn dẹp các chốt chặn tự động của Model
            address.UserId = user.Id;
            ModelState.Remove("UserId");
            ModelState.Remove("User");

            if (ModelState.IsValid)
            {
                var hasAddress = await _context.UserAddresses.AnyAsync(a => a.UserId == user.Id);
                if (!hasAddress)
                {
                    address.IsDefault = true;
                }
                else if (address.IsDefault)
                {
                    var defaults = await _context.UserAddresses.Where(a => a.UserId == user.Id && a.IsDefault).ToListAsync();
                    foreach (var d in defaults) d.IsDefault = false;
                }

                _context.UserAddresses.Add(address);
                await _context.SaveChangesAsync();

                TempData["success"] = "Đã thêm địa chỉ giao hàng thành công.";

                // Thành công mỹ mãn -> Điều hướng an toàn về trang danh sách
                return RedirectToAction(nameof(Index));
            }

            // ── 🔥 SỬA VỊ TRÍ NÀY: Nếu form lỗi, giữ chân khách lại trang Create kèm object lỗi
            TempData["error"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra các trường đánh dấu *";

            return View("Create", address);
        }

        // ── 5. SỔ ĐỊA CHỈ: SỬA ĐỊA CHỈ ───────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAddress(UserAddress address)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var existing = await _context.UserAddresses.FirstOrDefaultAsync(a => a.Id == address.Id && a.UserId == user.Id);
            if (existing != null)
            {
                existing.FullName = address.FullName;
                existing.PhoneNumber = address.PhoneNumber;
                existing.DetailedAddress = address.DetailedAddress;

                if (address.IsDefault && !existing.IsDefault)
                {
                    var defaults = await _context.UserAddresses.Where(a => a.UserId == user.Id && a.IsDefault).ToListAsync();
                    foreach (var d in defaults) d.IsDefault = false;
                    existing.IsDefault = true;
                }

                await _context.SaveChangesAsync();
                TempData["success"] = "Cập nhật địa chỉ thành công.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ── 6. SỔ ĐỊA CHỈ: XÓA ĐỊA CHỈ VÀ TỰ ĐỘNG ĐỔI CỜ CÒN LẠI ───────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var existing = await _context.UserAddresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == user.Id);
            if (existing != null)
            {
                _context.UserAddresses.Remove(existing);
                await _context.SaveChangesAsync();

                if (existing.IsDefault)
                {
                    var first = await _context.UserAddresses.FirstOrDefaultAsync(a => a.UserId == user.Id);
                    if (first != null)
                    {
                        first.IsDefault = true;
                        await _context.SaveChangesAsync();
                    }
                }
                TempData["success"] = "Đã xóa địa chỉ.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ── 7. SỔ ĐỊA CHỈ: ĐẶT LÀM ĐỊA CHỈ MẶC ĐỊNH ───────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDefaultAddress(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var existing = await _context.UserAddresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == user.Id);
            if (existing != null && !existing.IsDefault)
            {
                var defaults = await _context.UserAddresses.Where(a => a.UserId == user.Id && a.IsDefault).ToListAsync();
                foreach (var d in defaults) d.IsDefault = false;

                existing.IsDefault = true;
                await _context.SaveChangesAsync();
                TempData["success"] = "Đã thay đổi địa chỉ mặc định.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}