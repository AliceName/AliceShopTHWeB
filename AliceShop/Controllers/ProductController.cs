using AliceShop.Models;
using AliceShop.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace AliceShop.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;

        public ProductController(IProductRepository productRepository, ICategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
        }

        // ── 1. Collection (Bộ sưu tập mua sắm phía Khách hàng) ──
        public async Task<IActionResult> Collection(string? q)
        {
            IEnumerable<Product> products;

            if (!string.IsNullOrEmpty(q))
            {
                products = await _productRepository.SearchByNameAsync(q.Trim());
                ViewBag.SearchKeyword = q;
            }
            else
            {
                products = await _productRepository.GetAllAsync();
            }

            ViewBag.CategoriesList = await _categoryRepository.GetAllAsync();
            return View(products);
        }

        // ── 2. Details (Xem chi tiết sản phẩm phía Khách hàng) ──
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _productRepository.GetByIdAsync(id.Value);
            if (product == null) return NotFound();

            ViewBag.CategoriesList = await _categoryRepository.GetAllAsync();

            // ── LOGIC BÁN CHÉO (CROSS-SELLING) ───────────────────
            // 1. Lấy toàn bộ danh sách sản phẩm từ Repository
            var allProducts = await _productRepository.GetAllAsync();

            // 2. Lọc danh sách: Chọn các sản phẩm có cùng CategoryId nhưng loại bỏ sản phẩm hiện tại (id)
            var relatedProductsList = allProducts
                .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id)
                .Take(4) // Giới hạn lấy đúng 4 tuyệt tác để hiển thị vừa vặn trên 1 dòng Grid của giao diện
                .ToList();

            // 3. Đóng gói danh sách gửi sang tệp tin Views/Product/Details.cshtml
            ViewBag.RelatedProducts = relatedProductsList;
            // ──────────────────────────────────────────────────────────────────────────────

            return View(product);
        }
    }
}