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
    [Authorize(Roles = "Admin")] // 🔥 Khóa bảo mật: Chỉ tài khoản quyền Admin mới được phép thao tác
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

        // ── 1. Index: Giao diện quản lý danh sách chất liệu của Admin ──
        public async Task<IActionResult> Index()
        {
            var materials = await _materialRepository.GetAllAsync();
            return View(materials); // Tìm file Areas/Admin/Views/Material/Index.cshtml
        }

        // ── 2. Create (GET) ──
        public IActionResult Create()
        {
            return View(); // Tìm file Areas/Admin/Views/Material/Create.cshtml
        }

        // ── 2. Create (POST) ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name")] Material material)
        {
            ModelState.Remove("Products");

            if (ModelState.IsValid)
            {
                await _materialRepository.AddAsync(material);
                return RedirectToAction(nameof(Index));
            }

            return View(material);
        }

        // ── 3. Details ──
        public async Task<IActionResult> Details(int id)
        {
            var material = await _materialRepository.GetByIdAsync(id);
            if (material == null) return NotFound();

            return View(material); // Tìm file Areas/Admin/Views/Material/Details.cshtml
        }

        // ── 4. Edit (GET) ──
        public async Task<IActionResult> Edit(int id)
        {
            var material = await _materialRepository.GetByIdAsync(id);
            if (material == null) return NotFound();

            return View(material); // Tìm file Areas/Admin/Views/Material/Edit.cshtml
        }

        // ── 4. Edit (POST) ──
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

        // ── 5. Delete (GET) ──
        public async Task<IActionResult> Delete(int id)
        {
            var material = await _materialRepository.GetByIdAsync(id);
            if (material == null) return NotFound();

            // Đếm số lượng sản phẩm đang dùng chất liệu này để hiển thị cảnh báo
            var allProducts = await _productRepository.GetAllAsync();
            var productsWithMaterial = allProducts.Where(p => p.MaterialId == id).ToList();
            ViewBag.ProductCount = productsWithMaterial.Count;

            return View(material); // Tìm file Areas/Admin/Views/Material/Delete.cshtml
        }

        // ── 5. Delete (POST) ──
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var allProducts = await _productRepository.GetAllAsync();
            var productsWithMaterial = allProducts.Where(p => p.MaterialId == id).ToList();

            // Dọn dẹp tệp ảnh vật lý và xóa sản phẩm liên kết tránh lỗi Foreign Key Constraint
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
                await _productRepository.DeleteAsync(p.Id);
            }

            await _materialRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}