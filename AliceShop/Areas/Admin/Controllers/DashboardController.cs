using System;
using System.Linq;
using System.Threading.Tasks;
using AliceShop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace AliceShop.Areas.Admin.Controllers
{
    public class ProductPerformanceVM
    {
        public Product Product { get; set; }
        public string CategoryName { get; set; }
        public int TotalSold { get; set; }
        public decimal Revenue { get; set; }
        public int TotalStock { get; set; }
        public string ImageUrl { get; set; }
    }

    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string filter = "ThisMonth")
        {
            ViewBag.CurrentFilter = filter;
            
            DateTime startDate = DateTime.MinValue;
            DateTime endDate = DateTime.Now;

            // Lọc theo thời gian
            if (filter == "Today")
            {
                startDate = DateTime.Today;
            }
            else if (filter == "ThisWeek")
            {
                // Thứ 2 là ngày bắt đầu tuần
                int diff = (7 + (DateTime.Now.DayOfWeek - DayOfWeek.Monday)) % 7;
                startDate = DateTime.Today.AddDays(-1 * diff).Date;
            }
            else // ThisMonth
            {
                startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            }

            // 1. Lấy đơn hàng trong khoảng thời gian (Tất cả trạng thái)
            var orders = await _context.Orders
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                .ToListAsync();

            var completedOrders = orders.Where(o => o.Status == "Completed").ToList();

            // 2. Tính toán KPI
            var totalOrders = orders.Count;
            var totalRevenue = completedOrders.Sum(o => o.TotalAmount);
            var averageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;
            var conversionRate = 3.42m; // Mocked conversion rate do không có dữ liệu pageviews

            ViewBag.TotalOrders = totalOrders;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.AverageOrderValue = averageOrderValue;
            ViewBag.ConversionRate = conversionRate;

            // 3. Chuẩn bị dữ liệu Line Chart (Doanh thu theo thời gian)
            List<string> labels = new List<string>();
            List<decimal> data = new List<decimal>();

            if (filter == "Today")
            {
                var hourlyRevenue = completedOrders
                    .GroupBy(o => o.OrderDate.Hour)
                    .ToDictionary(g => g.Key, g => g.Sum(o => o.TotalAmount));

                for (int i = 0; i <= DateTime.Now.Hour; i++)
                {
                    labels.Add($"{i}:00");
                    data.Add(hourlyRevenue.ContainsKey(i) ? hourlyRevenue[i] : 0);
                }
            }
            else if (filter == "ThisWeek")
            {
                var dailyRevenue = completedOrders
                    .GroupBy(o => o.OrderDate.Date)
                    .ToDictionary(g => g.Key, g => g.Sum(o => o.TotalAmount));

                for (int i = 0; i < 7; i++)
                {
                    var d = startDate.AddDays(i);
                    if (d > endDate) break;
                    labels.Add(d.ToString("dd/MM"));
                    data.Add(dailyRevenue.ContainsKey(d.Date) ? dailyRevenue[d.Date] : 0);
                }
            }
            else
            {
                var dailyRevenue = completedOrders
                    .GroupBy(o => o.OrderDate.Date)
                    .ToDictionary(g => g.Key, g => g.Sum(o => o.TotalAmount));

                int daysInMonth = DateTime.DaysInMonth(startDate.Year, startDate.Month);
                for (int i = 0; i < daysInMonth; i++)
                {
                    var d = startDate.AddDays(i);
                    labels.Add(d.ToString("dd/MM"));
                    data.Add(dailyRevenue.ContainsKey(d.Date) ? dailyRevenue[d.Date] : 0);
                    if (d.Date == endDate.Date) break;
                }
            }

            ViewBag.ChartLabels = labels;
            ViewBag.ChartData = data;

            // 4. Lấy chi tiết đơn hàng hoàn thành để phân tích 
            var orderDetailsWithinRange = await _context.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.ProductSizeVariant)
                    .ThenInclude(psv => psv.Product)
                        .ThenInclude(p => p.Category)
                .Include(od => od.ProductSizeVariant)
                    .ThenInclude(psv => psv.Product)
                        .ThenInclude(p => p.ProductSizeVariants) // Để lấy số lượng tồn kho
                .Include(od => od.ProductSizeVariant)
                    .ThenInclude(psv => psv.Product)
                        .ThenInclude(p => p.Images) // Để lấy ảnh hiển thị
                .Where(od => od.Order.OrderDate >= startDate && od.Order.OrderDate <= endDate && od.Order.Status == "Completed")
                .ToListAsync();

            // 5. Doanh thu theo danh mục (Ring Chart)
            var categoryRevenue = orderDetailsWithinRange
                .Where(od => od.ProductSizeVariant?.Product?.Category != null)
                .GroupBy(od => od.ProductSizeVariant.Product.Category.Name)
                .Select(g => new {
                    CategoryName = g.Key,
                    Revenue = g.Sum(od => od.Price * od.Quantity)
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            ViewBag.CategoryLabels = categoryRevenue.Select(c => c.CategoryName).ToList();
            ViewBag.CategoryData = categoryRevenue.Select(c => c.Revenue).ToList();

            // 6. Top 3 Sản phẩm bán chạy nhất
            var topProducts = orderDetailsWithinRange
                .Where(od => od.ProductSizeVariant?.Product != null)
                .GroupBy(od => od.ProductSizeVariant.Product)
                .Select(g => new ProductPerformanceVM {
                    Product = g.Key,
                    CategoryName = g.Key.Category?.Name ?? "Khác",
                    TotalSold = g.Sum(od => od.Quantity),
                    Revenue = g.Sum(od => od.Price * od.Quantity),
                    ImageUrl = g.Key.ImageUrl ?? g.Key.Images?.FirstOrDefault()?.Url ?? "/images/placeholder.png",
                    TotalStock = g.Key.ProductSizeVariants?.Sum(v => v.StockQuantity) ?? 0
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(3)
                .ToList();

            ViewBag.TopProducts = topProducts;

            // 7. Bảng Phân tích hiệu suất sản phẩm (Top 10 theo doanh thu)
            var productPerformance = orderDetailsWithinRange
                .Where(od => od.ProductSizeVariant?.Product != null)
                .GroupBy(od => od.ProductSizeVariant.Product)
                .Select(g => new ProductPerformanceVM {
                    Product = g.Key,
                    CategoryName = g.Key.Category?.Name ?? "Khác",
                    TotalSold = g.Sum(od => od.Quantity),
                    Revenue = g.Sum(od => od.Price * od.Quantity),
                    TotalStock = g.Key.ProductSizeVariants?.Sum(v => v.StockQuantity) ?? 0,
                    ImageUrl = g.Key.ImageUrl ?? g.Key.Images?.FirstOrDefault()?.Url ?? "/images/placeholder.png"
                })
                .OrderByDescending(x => x.Revenue)
                .Take(10)
                .ToList();

            ViewBag.ProductPerformance = productPerformance;

            return View();
        }
    }
}
