using AliceShop.Models;
using AliceShop.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AliceShop.Controllers
{
    public class MaterialController : Controller
    {
        private readonly IMaterialRepository _materialRepository;
        private readonly IProductRepository _productRepository;
        private readonly IWebHostEnvironment _env;

        public MaterialController(IMaterialRepository materialRepository, IProductRepository productRepository, IWebHostEnvironment env)
        {
            _materialRepository = materialRepository;
            _productRepository = productRepository;
            _env = env;
        }

        // ── 1. Index: Giao diện quản lý danh sách chất liệu ─────────────────
        public async Task<IActionResult> Index()
        {
            var materials = await _materialRepository.GetAllAsync();
            return View(materials); // Tìm file Views/Material/Index.cshtml
        }

        // ── 2. Create (GET): Giao diện thêm chất liệu mới ──────────────────
        public IActionResult Create()
        {
            return View(); // Tìm file Views/Material/Create.cshtml
        }

        // ── 2. Create (POST): Xử lý lưu chất liệu vào SQL Server ─────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name")] Material material)
        {
            // Bỏ qua xác thực danh sách sản phẩm liên kết để tránh lỗi ModelState ngầm
            ModelState.Remove("Products");

            if (ModelState.IsValid)
            {
                await _materialRepository.AddAsync(material);
                return RedirectToAction(nameof(Index));
            }

            return View(material);
        }

        // ── 3. Details: Xem thông tin chi tiết chất liệu ──────────────────
        public async Task<IActionResult> Details(int id)
        {
            var material = await _materialRepository.GetByIdAsync(id);
            if (material == null) return NotFound();

            return View(material); // Tìm file Views/Material/Details.cshtml
        }

        // ── 4. Edit (GET): Giao diện chỉnh sửa tên chất liệu ───────────────
        public async Task<IActionResult> Edit(int id)
        {
            var material = await _materialRepository.GetByIdAsync(id);
            if (material == null) return NotFound();

            return View(material); // Tìm file Views/Material/Edit.cshtml
        }

        // ── 4. Edit (POST): Xử lý lưu cập nhật ─────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] Material material)
        {
            if (id != material.Id) return NotFound();

            ModelState.Remove("Products");

            if (ModelState.IsValid)
            {
                var existing = await _materialRepository.GetByIdAsync(id);
                if (existing == null) return NotFound();

                existing.Name = material.Name;
                await _materialRepository.UpdateAsync(existing);
                return RedirectToAction(nameof(Index));
            }

            return View(material);
        }

        // ── 5. Delete (GET): Giao diện cảnh báo xác nhận xóa chất liệu ─────
        public async Task<IActionResult> Delete(int id)
        {
            var material = await _materialRepository.GetByIdAsync(id);
            if (material == null) return NotFound();

            // Đếm số lượng sản phẩm đang dùng chất liệu này để hiển thị cảnh báo cho Admin
            var allProducts = await _productRepository.GetAllAsync();
            var productsWithMaterial = allProducts.Where(p => p.MaterialId == id).ToList();
            ViewBag.ProductCount = productsWithMaterial.Count;

            return View(material); // Tìm file Views/Material/Delete.cshtml
        }

        // ── 5. Delete (POST): Thực thi quy trình dọn rác và xóa dữ liệu ──────
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var allProducts = await _productRepository.GetAllAsync();
            var productsWithMaterial = allProducts.Where(p => p.MaterialId == id).ToList();

            // Quy trình dọn rác: Xóa file vật lý của tất cả ảnh phụ sản phẩm thuộc chất liệu này trong wwwroot
            foreach (var p in productsWithMaterial)
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
                // Xóa sản phẩm con ra khỏi SQL Server trước để tránh lỗi dính khóa ngoại (Foreign Key Constraint)
                await _productRepository.DeleteAsync(p.Id);
            }

            // Tiến hành xóa gốc bản ghi chất liệu cha
            await _materialRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}