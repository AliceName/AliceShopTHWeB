using AliceShop.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AliceShop.Controllers
{
    public class HomeController : Controller
    {
        private readonly IProductRepository _productRepository;

        // Inject IProductRepository vào Controller trang ch?
        public HomeController(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<IActionResult> Index()
        {
            var allProducts = await _productRepository.GetAllAsync();

            var topProducts = allProducts.OrderByDescending(p => p.Id).Take(4).ToList();

            return View(topProducts);
        }
        // ── 4. Contact (Trang liên hệ khách hàng Quiet Luxury) ──────────────────────
        public IActionResult Contact()
        {
            // Trả về View tương ứng tại đường dẫn Views/Home/Contact.cshtml
            return View();
        }
    }
}