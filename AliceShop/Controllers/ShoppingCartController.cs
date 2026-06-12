using System;
using System.Collections.Generic;
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
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>(CART_SESSION_KEY) ?? new ShoppingCart();

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

            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>(CART_SESSION_KEY) ?? new ShoppingCart();
            cart.AddItem(cartItem);
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
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>(CART_SESSION_KEY);

            if (cart is not null)
            {
                cart.RemoveItem(sizeVariantId);
                HttpContext.Session.SetObjectAsJson(CART_SESSION_KEY, cart);
            }

            return RedirectToAction("Index");
        }

        // ── 4. UPDATE QUANTITY: CẬP NHẬT SỐ LƯỢNG CHO ĐỒNG BỘ FORM ────────────────────
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int sizeVariantId, int quantity)
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>(CART_SESSION_KEY);

            if (cart is not null && quantity > 0)
            {
                var variant = await _context.ProductSizeVariants.FindAsync(sizeVariantId);
                if (variant != null && variant.StockQuantity >= quantity)
                {
                    cart.UpdateQuantity(sizeVariantId, quantity);
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

            foreach (var item in cart.Items)
            {
                var variant = await _context.ProductSizeVariants
                    .Include(pv => pv.Product)
                    .FirstOrDefaultAsync(pv => pv.Id == item.ProductSizeVariantId);
                if (variant != null) item.ProductSizeVariant = variant;
            }

            ViewBag.Cart = cart;

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

            return View(orderInit);
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
                var user = await _userManager.GetUserAsync(User);
                order.UserId = user?.Id;
                order.OrderDate = DateTime.Now;
                order.TotalAmount = cart.TotalAmount;

                // 🔥 ĐỒNG BỘ KIẾN TRÚC: Tất cả đơn hàng mới tạo đều ở tiến độ "Chờ phê duyệt"
                order.Status = "Pending";

                // Thiết lập trạng thái dòng tiền dựa theo phương thức thanh toán khách chọn
                if (order.PaymentMethod == "COD" || order.PaymentMethod == "BankTransfer")
                {
                    order.PaymentStatus = "Unpaid"; // Tiền mặt hoặc quét mã VietQR thủ công đều là chưa thu tiền
                }
                else
                {
                    order.PaymentStatus = "Unpaid"; // Thanh toán online cũng đặt tạm Unpaid, khi đối tác duyệt mới đổi sang Paid
                }

                order.OrderDetails = cart.Items.Select(item => new OrderDetail
                {
                    ProductSizeVariantId = item.ProductSizeVariantId,
                    Quantity = item.Quantity,
                    Price = item.ProductSizeVariant.Price
                }).ToList();

                // Kiểm tra và thực hiện trừ kho thực tế dưới SQL Server
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
                        dbVariant.StockQuantity -= item.Quantity;
                    }
                }

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // ── 🔥 RẼ NHÁNH ĐIỀU HƯỚNG ───────────────────────────────────────────
                if (order.PaymentMethod == "CreditCard" || order.PaymentMethod == "EWallet")
                {
                    // Chuyển hướng sang trạm trung gian quẹt thẻ / ví để thu tiền trước, giỏ hàng giữ nguyên
                    return RedirectToAction("ProcessOnlinePayment", new { orderId = order.Id, method = order.PaymentMethod });
                }

                // Nếu là BankTransfer hoặc COD -> Xóa sạch túi hàng Session vì đơn chờ đã được ghi nhận an toàn
                HttpContext.Session.Remove(CART_SESSION_KEY);
                return View("OrderCompleted", order.Id);
            }

            ViewBag.Cart = cart;
            return View(order);
        }

        // ── 7. XỬ LÝ TRẠM ENDPOINT: CHỈ KHI THANH TOÁN XONG MỚI BÁO THÀNH CÔNG VÀ XÓA GIỎ HÀNG ──
        [Authorize]
        public async Task<IActionResult> ProcessOnlinePayment(int orderId, string method)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            // Giả lập cổng thanh toán trực tuyến trả về kết quả thành công thành công (SUCCESS)
            bool isPaymentSuccess = true;

            if (isPaymentSuccess)
            {
                //  Khớp lệnh thành công -> Chuyển cột tiền sang Paid. 
                // Cột tiến độ Status giữ nguyên là "Pending" (Chờ Admin bọc hộp Premium mang giao).
                order.PaymentStatus = "Paid";
                _context.Orders.Update(order);
                await _context.SaveChangesAsync();

                // Dọn sạch túi hàng Session của khách sau khi đã tất toán hóa đơn an toàn
                HttpContext.Session.Remove(CART_SESSION_KEY);

                ViewBag.PaymentMethodUsed = method == "CreditCard" ? "Thẻ quốc tế Visa/Mastercard" : "Ví điện tử thông minh";
                return View("OrderCompleted", orderId);
            }
            else
            {
                TempData["ErrorMessage"] = "Giao dịch thanh toán trực tuyến không thành công. Vui lòng thử lại.";
                return RedirectToAction("Index");
            }
        }

        // ── 8. RE-ORDER: TÍNH NĂNG ĐẶT LẠI ĐƠN HÀNG CŨ CHO KHÁCH HÀNG ───────────────────
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ReOrder(int orderId)
        {
            var oldOrder = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (oldOrder == null) return NotFound("Không tìm thấy dữ liệu đơn hàng cũ.");

            // Lấy giỏ hàng hiện tại trong Session ra hoặc sinh mới nếu chưa có
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>(CART_SESSION_KEY) ?? new ShoppingCart();

            foreach (var detail in oldOrder.OrderDetails)
            {
                var variant = await _context.ProductSizeVariants
                    .Include(pv => pv.Product)
                    .Include(pv => pv.ProductSize)
                    .FirstOrDefaultAsync(pv => pv.Id == detail.ProductSizeVariantId);

                if (variant != null && variant.StockQuantity > 0)
                {
                    // Tính toán số lượng tối đa có thể mua dựa vào kho thực tế còn lại
                    int purchaseQuantity = Math.Min(detail.Quantity, variant.StockQuantity);

                    var cartItem = new CartItem
                    {
                        ProductSizeVariantId = detail.ProductSizeVariantId,
                        ProductSizeVariant = variant,
                        Quantity = purchaseQuantity
                    };

                    cart.AddItem(cartItem);
                }
            }

            // Lưu lại tổ hợp túi hàng mới vào Session
            HttpContext.Session.SetObjectAsJson(CART_SESSION_KEY, cart);

            TempData["SuccessMessage"] = "Đã tự động tái nạp toàn bộ tuyệt tác từ đơn hàng cũ vào túi hàng hiện hành của bạn!";
            return RedirectToAction("Index");
        }
    }
}