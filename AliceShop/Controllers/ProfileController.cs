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

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var addresses = await _context.UserAddresses
                .Where(a => a.UserId == user.Id)
                .OrderByDescending(a => a.IsDefault)
                .ToListAsync();

            return View(addresses);
        }

        [HttpGet]
        public IActionResult CreateAddress()
        {
            return View("Create");
        }

        [HttpGet]
        public async Task<IActionResult> Orders()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.ProductSizeVariant)
                .ThenInclude(psv => psv.Product)
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAddress(UserAddress address)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

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
            }
            else
            {
                TempData["error"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
            }
            return RedirectToAction(nameof(Index));
        }

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
                
                // Set the first remaining address to default if needed
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
