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

        public async Task<IActionResult> Index(string status)
        {
            var ordersQuery = _context.Orders
                .Include(o => o.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                ordersQuery = ordersQuery.Where(o => o.Status == status);
            }

            var orders = await ordersQuery.OrderByDescending(o => o.OrderDate).ToListAsync();
            
            ViewBag.CurrentStatus = status ?? "All";
            return View(orders);
        }

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

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.Status = status;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã cập nhật trạng thái đơn hàng #{order.Id} thành: {status}";
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }
    }
}
