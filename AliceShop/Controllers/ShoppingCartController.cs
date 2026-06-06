using System.Linq;
using System.Threading.Tasks;
using AliceShop.Extensions; // Kích hoạt Extension để nhận diện SetObjectAsJson / GetObjectFromJson
using AliceShop.Models;
using AliceShop.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AliceShop.Controllers
{
    public class ShoppingCartController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private const string CART_SESSION_KEY = "Cart";

        public ShoppingCartController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IProductRepository productRepository)
        {
            _productRepository = productRepository;
            _context = context;
            _userManager = userManager;
        }

        // ── 1. INDEX: GIAO DIỆN GIỎ HÀNG KHÁCH HÀNG ───────────────────────────────────
        public async Task<IActionResult> Index()
        {
            // 🔥 ĐỒNG BỘ: Sửa thành GetObjectFromJson theo file Extension của Ngà
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>(CART_SESSION_KEY) ?? new ShoppingCart();

            // Nạp lại thông tin Product và SizeName từ SQL Server để tránh lỗi Null ngoài giao diện
            if (cart.Items.Any())
            {
                foreach (var item in cart.Items)
                {
                    var variant = await _context.ProductSizeVariants
                        .Include(pv => pv.Product)
                        .Include(pv => pv.ProductSize)
                        .FirstOrDefaultAsync(pv => pv.Id == item.ProductSizeVariantId);

                    if (variant != null)
                    {
                        item.ProductSizeVariant = variant;
                    }
                }
            }

            return View(cart);
        }

        // ── 2. ADD TO CART: TIẾP NHẬN BIẾN THỂ SIZE VÀ KIỂM KHO ────────────────────────
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddToCart(int sizeVariantId, int quantity)
        {
            if (quantity <= 0) quantity = 1;

            var variant = await _context.ProductSizeVariants
                .Include(pv => pv.Product)
                .Include(pv => pv.ProductSize)
                .FirstOrDefaultAsync(pv => pv.Id == sizeVariantId);

            if (variant == null) return NotFound("Biến thể kích cỡ trang sức không tồn tại.");

            if (variant.StockQuantity < quantity)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = $"Mẫu này chỉ còn {variant.StockQuantity} sản phẩm thuộc kích cỡ {variant.ProductSize.SizeName}." });
                }
                TempData["ErrorMessage"] = $"Mẫu này chỉ còn {variant.StockQuantity} sản phẩm thuộc kích cỡ {variant.ProductSize.SizeName}.";
                return RedirectToAction("Details", "Product", new { id = variant.ProductId });
            }

            var cartItem = new CartItem
            {
                ProductSizeVariantId = sizeVariantId,
                ProductSizeVariant = variant,
                Quantity = quantity
            };

            // 🔥 ĐỒNG BỘ: Sửa thành GetObjectFromJson theo file Extension của Ngà
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>(CART_SESSION_KEY) ?? new ShoppingCart();

            cart.AddItem(cartItem);

            // 🔥 ĐỒNG BỘ: Sửa thành SetObjectAsJson theo file Extension của Ngà
            HttpContext.Session.SetObjectAsJson(CART_SESSION_KEY, cart);

            var successMsg = $"Đã thêm {variant.Product.Name} ({variant.ProductSize.SizeName}) vào túi hàng.";

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = successMsg, cartCount = cart.TotalQuantity });
            }

            TempData["SuccessMessage"] = successMsg;
            return RedirectToAction("Index");
        }

        // ── 2b. BUY NOW: THÊM VÀO GIỎ VÀ ĐI THẲNG TỚI TRANG THANH TOÁN ─────────────────
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> BuyNow(int sizeVariantId, int quantity)
        {
            if (quantity <= 0) quantity = 1;

            var variant = await _context.ProductSizeVariants
                .Include(pv => pv.Product)
                .Include(pv => pv.ProductSize)
                .FirstOrDefaultAsync(pv => pv.Id == sizeVariantId);

            if (variant == null) return NotFound("Biến thể kích cỡ trang sức không tồn tại.");

            if (variant.StockQuantity < quantity)
            {
                TempData["ErrorMessage"] = $"Mẫu này chỉ còn {variant.StockQuantity} sản phẩm thuộc kích cỡ {variant.ProductSize.SizeName}.";
                return RedirectToAction("Details", "Product", new { id = variant.ProductId });
            }

            var cartItem = new CartItem
            {
                ProductSizeVariantId = sizeVariantId,
                ProductSizeVariant = variant,
                Quantity = quantity
            };

            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>(CART_SESSION_KEY) ?? new ShoppingCart();
            cart.AddItem(cartItem);
            HttpContext.Session.SetObjectAsJson(CART_SESSION_KEY, cart);

            return RedirectToAction("Checkout");
        }

        // ── 3. REMOVE FROM CART: XÓA MÓN DỰA THEO MÃ BIẾN THỂ KÍCH CỠ ──────────────────
        public IActionResult RemoveFromCart(int sizeVariantId)
        {
            // 🔥 ĐỒNG BỘ: Sửa thành GetObjectFromJson theo file Extension của Ngà
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>(CART_SESSION_KEY);

            if (cart is not null)
            {
                cart.RemoveItem(sizeVariantId);

                // 🔥 ĐỒNG BỘ: Sửa thành SetObjectAsJson theo file Extension của Ngà
                HttpContext.Session.SetObjectAsJson(CART_SESSION_KEY, cart);
            }

            return RedirectToAction("Index");
        }

        // ── 4. UPDATE QUANTITY: CẬP NHẬT SỐ LƯỢNG CHO ĐỒNG BỘ FORM ────────────────────
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int sizeVariantId, int quantity)
        {
            // 🔥 ĐỒNG BỘ: Sửa thành GetObjectFromJson theo file Extension của Ngà
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>(CART_SESSION_KEY);

            if (cart is not null && quantity > 0)
            {
                var variant = await _context.ProductSizeVariants.FindAsync(sizeVariantId);
                if (variant != null && variant.StockQuantity >= quantity)
                {
                    cart.UpdateQuantity(sizeVariantId, quantity);

                    // 🔥 ĐỒNG BỘ: Sửa thành SetObjectAsJson theo file Extension của Ngà
                    HttpContext.Session.SetObjectAsJson(CART_SESSION_KEY, cart);
                }
            }
            return RedirectToAction("Index");
        }

        // ── 5. CHECKOUT (GET): HIỂN THỊ TRANG NHẬP ĐỊA CHỈ ĐẶT HÀNG ───────────────────
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Checkout()
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>(CART_SESSION_KEY);
            if (cart == null || cart.IsEmpty)
            {
                TempData["ErrorMessage"] = "Túi hàng của bạn đang trống, không thể tiến hành đặt hàng.";
                return RedirectToAction("Index");
            }

            // Đồng bộ dữ liệu để tính toán tổng tiền hiển thị chính xác
            foreach (var item in cart.Items)
            {
                var variant = await _context.ProductSizeVariants
                    .Include(pv => pv.Product)
                    .FirstOrDefaultAsync(pv => pv.Id == item.ProductSizeVariantId);
                if (variant != null) item.ProductSizeVariant = variant;
            }

            // Truyền tổng tiền sang để hiển thị tóm tắt hóa đơn
            ViewBag.Cart = cart;

            // Tự động bốc Email hoặc thông tin mặc định của User đã đăng nhập (Nếu có) làm tiền đề cho sổ địa chỉ sau này
            var user = await _userManager.GetUserAsync(User);
            var orderInit = new Order
            {
                FullName = user?.FullName ?? ""
            };

            if (user != null)
            {
                var addresses = await _context.UserAddresses
                    .Where(a => a.UserId == user.Id)
                    .OrderByDescending(a => a.IsDefault)
                    .ToListAsync();
                
                ViewBag.Addresses = addresses;

                var defaultAddress = addresses.FirstOrDefault(a => a.IsDefault) ?? addresses.FirstOrDefault();
                if (defaultAddress != null)
                {
                    orderInit.FullName = defaultAddress.FullName;
                    orderInit.PhoneNumber = defaultAddress.PhoneNumber;
                    orderInit.ShippingAddress = defaultAddress.DetailedAddress;
                }
            }

            return View(orderInit); // Nạp file Views/ShoppingCart/Checkout.cshtml
        }

        // ── 6. CHECKOUT (POST): XỬ LÝ LƯU HÓA ĐƠN ĐƠN HÀNG VÀ TRỪ KHO THỰC TẾ ─────────
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(Order order)
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>(CART_SESSION_KEY);
            if (cart == null || cart.IsEmpty)
            {
                ModelState.AddModelError("", "Túi hàng trống, vui lòng thêm sản phẩm trước khi thanh toán.");
                return RedirectToAction("Index");
            }

            // Nạp lại dữ liệu biến thể để chốt giá đóng băng lịch sử và kiểm tra kho lần cuối
            foreach (var item in cart.Items)
            {
                var variant = await _context.ProductSizeVariants
                    .Include(pv => pv.Product)
                    .Include(pv => pv.ProductSize)
                    .FirstOrDefaultAsync(pv => pv.Id == item.ProductSizeVariantId);

                if (variant == null)
                {
                    ModelState.AddModelError("", "Một số tác phẩm trong giỏ hàng đã không còn tồn tại trên hệ thống.");
                    ViewBag.Cart = cart;
                    return View(order);
                }
                item.ProductSizeVariant = variant;
            }

            if (ModelState.IsValid)
            {
                // 1. Gán các thông tin hệ thống cho đơn hàng
                var user = await _userManager.GetUserAsync(User);
                order.UserId = user?.Id;
                order.OrderDate = DateTime.Now;
                order.Status = "Pending"; // Đặt trạng thái chờ duyệt ban đầu
                order.TotalAmount = cart.TotalAmount; // Ghi nhận số tiền chốt từ thuộc tính TotalAmount của ShoppingCart

                // 2. Chuyển đổi toàn bộ item trong giỏ hàng sang bảng chi tiết hóa đơn OrderDetails
                order.OrderDetails = cart.Items.Select(item => new OrderDetail
                {
                    ProductSizeVariantId = item.ProductSizeVariantId, // Khóa ngoại bám chuẩn biến thể kích cỡ
                    Quantity = item.Quantity,
                    Price = item.ProductSizeVariant.Price // Đóng băng giá gốc lịch sử bán
                }).ToList();

                // 3. ⚠️ QUAN TRỌNG: Vòng lặp trừ kho thực tế dưới SQL Server để tránh lỗi bán lố (Overselling)
                foreach (var item in cart.Items)
                {
                    var dbVariant = await _context.ProductSizeVariants.FindAsync(item.ProductSizeVariantId);
                    if (dbVariant != null)
                    {
                        if (dbVariant.StockQuantity < item.Quantity)
                        {
                            TempData["ErrorMessage"] = $"Tác phẩm {item.ProductSizeVariant.Product.Name} ({item.ProductSizeVariant.ProductSize.SizeName}) vừa hết hàng hoặc không đủ số lượng trong kho.";
                            return RedirectToAction("Index");
                        }
                        // Tiến hành khấu trừ số lượng kho thực tế của riêng Size này
                        dbVariant.StockQuantity -= item.Quantity;
                    }
                }

                // 4. Lưu toàn cục hóa đơn đơn hàng xuống Database SQL Server
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // 5. Xóa sạch giỏ hàng tạm thời trong Session sau khi đặt mua thành công
                HttpContext.Session.Remove(CART_SESSION_KEY);

                // Trả về View thông báo đặt hàng thành công và gửi kèm mã đơn hàng tự tăng
                return View("OrderCompleted", order.Id);
            }

            // Nếu dữ liệu form nhập địa chỉ bị lỗi, render lại trang Checkout kèm dữ liệu giỏ hàng để hiển thị
            ViewBag.Cart = cart;
            return View(order);
        }
    }
}