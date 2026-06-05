using AliceShop.Models;
using AliceShop.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AliceShop.Areas.Admin.Controllers
{
    [Area("Admin")] // 🔥 Định danh thuộc phân khu quản trị Admin Area
    [Authorize(Roles = "Admin")] // 🔥 Khóa bảo mật cấp lớp: Chỉ tài khoản nhóm Admin mới được truy cập
    public class CategoryController : Controller
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IProductRepository _productRepository;
        private readonly IWebHostEnvironment _env;

        public CategoryController(ICategoryRepository categoryRepository, IProductRepository productRepository, IWebHostEnvironment env)
        {
            _categoryRepository = categoryRepository;
            _productRepository = productRepository;
            _env = env;
        }

        // ── 1. Index: Giao diện danh sách danh mục của Admin ──
        public async Task<IActionResult> Index()
        {
            var categories = await _categoryRepository.GetAllAsync();
            return View(categories); // Nạp file Areas/Admin/Views/Category/Index.cshtml
        }

        // ── 2. Create (GET) ──
        public IActionResult Create()
        {
            return View(); // Nạp file Areas/Admin/Views/Category/Create.cshtml
        }

        // ── 2. Create (POST) ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            // Ép bỏ qua thuộc tính Products liên kết để tránh ModelState.IsValid bị False ngầm
            ModelState.Remove("Products");

            if (ModelState.IsValid)
            {
                await _categoryRepository.AddAsync(category);
                return RedirectToAction(nameof(Index));
            }

            return View(category);
        }

        // ── 3. Details ──
        public async Task<IActionResult> Details(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null) return NotFound();
            return View(category); // Nạp file Areas/Admin/Views/Category/Details.cshtml
        }

        // ── 4. Edit (GET) ──
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null) return NotFound();
            return View(category); // Nạp file Areas/Admin/Views/Category/Edit.cshtml
        }

        // ── 4. Edit (POST) ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category category)
        {
            if (id != category.Id) return NotFound();

            ModelState.Remove("Products");

            if (ModelState.IsValid)
            {
                var existing = await _categoryRepository.GetByIdAsync(id);
                if (existing == null) return NotFound();

                existing.Name = category.Name;
                await _categoryRepository.UpdateAsync(existing);
                return RedirectToAction(nameof(Index));
            }

            return View(category);
        }

        // ── 5. Delete (GET) ──
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null) return NotFound();

            // Đếm số lượng sản phẩm đang bị kẹt lại trong danh mục này để cảnh báo Admin
            var allProducts = await _productRepository.GetAllAsync();
            var productsInCategory = allProducts.Where(p => p.CategoryId == id).ToList();
            ViewBag.ProductCount = productsInCategory.Count;

            return View(category); // Nạp file Areas/Admin/Views/Category/Delete.cshtml
        }

        // ── 5. Delete (POST) ──
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var allProducts = await _productRepository.GetAllAsync();
            var productsInCategory = allProducts.Where(p => p.CategoryId == id).ToList();

            // Vòng lặp dọn rác vật lý: Xóa file ảnh trong thư mục wwwroot tránh tràn dữ liệu
            foreach (var p in productsInCategory)
            {
                if (p.Images != null)
                {
                    foreach (var img in p.Images)
                    {
                        var filePath = Path.Combine(_env.WebRootPath, img.Url.TrimStart('/'));
                        if (System.IO.File.Exists(filePath) && img.Url != "/images/placeholder.png")
                        {
                            System.IO.File.Delete(filePath);
                        }
                    }
                }
                // Xóa sản phẩm con chứa khóa ngoại trước
                await _productRepository.DeleteAsync(p.Id);
            }

            // Tiến hành xóa gốc danh mục cha ra khỏi SQL Server
            await _categoryRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}