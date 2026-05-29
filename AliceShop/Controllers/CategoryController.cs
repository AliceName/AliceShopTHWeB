using AliceShop.Models;
using AliceShop.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AliceShop.Controllers
{
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

        // 1. Giao diện danh sách danh mục
        public async Task<IActionResult> Index()
        {
            var categories = await _categoryRepository.GetAllAsync();
            return View(categories); // Nạp file Views/Category/Index.cshtml
        }

        // 2. Giao diện thêm danh mục (Đồng bộ tên từ "Add" sang "Create")
        public IActionResult Create()
        {
            return View(); // Nạp file Views/Category/Create.cshtml
        }

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

        // 3. Giao diện xem chi tiết danh mục (Đồng bộ tên từ "Display" sang "Details")
        public async Task<IActionResult> Details(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null) return NotFound();
            return View(category); // Nạp file Views/Category/Details.cshtml
        }

        // 4. Giao diện cập nhật thông tin (Đồng bộ tên từ "Update" sang "Edit")
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null) return NotFound();
            return View(category); // Nạp file Views/Category/Edit.cshtml
        }

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

        // 5. Giao diện xác nhận xóa danh mục
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null) return NotFound();

            // Đếm số lượng sản phẩm đang bị kẹt lại trong danh mục này để cảnh báo Admin
            var allProducts = await _productRepository.GetAllAsync();
            var productsInCategory = allProducts.Where(p => p.CategoryId == id).ToList();
            ViewBag.ProductCount = productsInCategory.Count;

            return View(category); // Nạp file Views/Category/Delete.cshtml
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var allProducts = await _productRepository.GetAllAsync();
            var productsInCategory = allProducts.Where(p => p.CategoryId == id).ToList();

            // Vòng lặp dọn rác cao cấp: Xóa file vật lý trong thư mục wwwroot để tránh tràn ổ cứng
            foreach (var p in productsInCategory)
            {
                if (p.Images != null)
                {
                    foreach (var img in p.Images)
                    {
                        var filePath = Path.Combine(_env.WebRootPath, img.Url.TrimStart('/'));
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                    }
                }
                // Xóa sản phẩm con trước
                await _productRepository.DeleteAsync(p.Id);
            }

            // Cuối cùng tiến hành xóa gốc danh mục cha ra khỏi SQL Server
            await _categoryRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}