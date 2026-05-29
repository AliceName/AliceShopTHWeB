using AliceShop.Models;
using AliceShop.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AliceShop.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IMaterialRepository _materialRepository;
        private readonly IWebHostEnvironment _env;

        public ProductController(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            IMaterialRepository materialRepository,
            IWebHostEnvironment env)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _materialRepository = materialRepository;
            _env = env;
        }

        // ── 1. Index (Quản lý dạng Card thời thượng của Admin) ──────────────
        public async Task<IActionResult> Index()
        {
            var products = await _productRepository.GetAllAsync();
            ViewBag.CategoriesList = await _categoryRepository.GetAllAsync();
            return View(products);
        }

        // ── 1b. Collection (Bộ sưu tập dạng Grid phía Khách hàng kết hợp tìm kiếm) ──
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

        // ── 2. Create (GET) ────────────────────────────────────────────────
        public async Task<IActionResult> Create()
        {
            await LoadDropdownsAsync();
            return View();
        }

        // ── 2. Create (POST) ───────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Price,Description,CategoryId,MaterialId")] Product product, IFormFile? imageUrl, List<IFormFile>? imageFiles)
        {
            // Loại bỏ các kiểm tra navigation phức hợp của EF Core để tránh lỗi ModelState không đáng có
            ModelState.Remove("ImageUrl");
            ModelState.Remove("Images");
            ModelState.Remove("Category");
            ModelState.Remove("Material");

            if (ModelState.IsValid)
            {
                product.Images = new List<ProductImage>();

                // 1. Xử lý lưu File ảnh đại diện chính (Khớp 100% với name="imageUrl" ở View)
                if (imageUrl != null && imageUrl.Length > 0)
                {
                    product.ImageUrl = await SaveImage(imageUrl);
                }
                else
                {
                    // Nếu Admin không tải ảnh, gán ảnh tạm chống vỡ khung hình
                    product.ImageUrl = "/images/placeholder.png";
                }

                // 2. Xử lý lưu danh sách tập hợp các ảnh góc chụp phụ con (ProductImage 1-N)
                if (imageFiles != null && imageFiles.Count > 0)
                {
                    foreach (var file in imageFiles.Where(f => f.Length > 0))
                    {
                        var savedPath = await SaveImage(file);
                        product.Images.Add(new ProductImage { Url = savedPath });
                    }
                }

                await _productRepository.AddAsync(product);
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdownsAsync(product.CategoryId, product.MaterialId);
            return View(product);
        }

        // ── 3. Details (Xem chi tiết sản phẩm) ─────────────────────────────
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _productRepository.GetByIdAsync(id.Value);
            if (product == null) return NotFound();

            return View(product);
        }

        // ── 4. Edit (GET) ──────────────────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();

            await LoadDropdownsAsync(product.CategoryId, product.MaterialId);
            return View(product);
        }

        // ── 4. Edit (POST) ─────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Price,Description,CategoryId,MaterialId")] Product product, List<IFormFile>? imageFiles, List<int>? removedImageIds)
        {
            if (id != product.Id) return NotFound();

            ModelState.Remove("ImageUrl");
            ModelState.Remove("Images");
            ModelState.Remove("Category");
            ModelState.Remove("Material");

            if (ModelState.IsValid)
            {
                var existing = await _productRepository.GetByIdAsync(id);
                if (existing == null) return NotFound();

                existing.Name = product.Name;
                existing.Price = product.Price;
                existing.Description = product.Description;
                existing.CategoryId = product.CategoryId;
                existing.MaterialId = product.MaterialId;

                // Xử lý loại bỏ ảnh phụ khi chọn xóa trên UI
                if (removedImageIds != null && removedImageIds.Count > 0 && existing.Images != null)
                {
                    var imagesToRemove = existing.Images.Where(img => removedImageIds.Contains(img.Id)).ToList();
                    foreach (var img in imagesToRemove)
                    {
                        var filePath = Path.Combine(_env.WebRootPath, img.Url.TrimStart('/'));
                        if (System.IO.File.Exists(filePath) && img.Url != "/images/placeholder.png")
                        {
                            System.IO.File.Delete(filePath);
                        }
                        existing.Images.Remove(img);
                    }

                    if (imagesToRemove.Any(img => img.Url == existing.ImageUrl))
                    {
                        existing.ImageUrl = existing.Images.FirstOrDefault()?.Url ?? "/images/placeholder.png";
                    }
                }

                // Tải lên thêm ảnh phụ mới
                if (imageFiles != null && imageFiles.Count > 0)
                {
                    existing.Images ??= new List<ProductImage>();
                    foreach (var file in imageFiles.Where(f => f.Length > 0))
                    {
                        var savedPath = await SaveImage(file);
                        existing.ImageUrl = savedPath; // Đẩy tấm ảnh mới nhất làm ảnh chính đại diện
                        existing.Images.Add(new ProductImage { Url = savedPath, ProductId = existing.Id });
                    }
                }

                await _productRepository.UpdateAsync(existing);
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdownsAsync(product.CategoryId, product.MaterialId);
            return View(product);
        }

        // ── 5. Delete (GET) ────────────────────────────────────────────────
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        // ── 5. Delete (POST) ───────────────────────────────────────────────
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product != null && product.Images != null)
            {
                foreach (var img in product.Images)
                {
                    var filePath = Path.Combine(_env.WebRootPath, img.Url.TrimStart('/'));
                    if (System.IO.File.Exists(filePath) && img.Url != "/images/placeholder.png")
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
            }

            await _productRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        // ── Helpers ────────────────────────────────────────────────────────
        private async Task<string> SaveImage(IFormFile image)
        {
            var ext = Path.GetExtension(image.FileName);
            var fileName = Guid.NewGuid().ToString("N") + ext;
            var folder = Path.Combine(_env.WebRootPath, "images", "products");

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var fullPath = Path.Combine(folder, fileName);
            await using var stream = new FileStream(fullPath, FileMode.Create);
            await image.CopyToAsync(stream);

            return "/images/products/" + fileName;
        }

        private async Task LoadDropdownsAsync(int? selectedCategoryId = null, int? selectedMaterialId = null)
        {
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", selectedCategoryId);

            var materials = await _materialRepository.GetAllAsync();
            ViewBag.Materials = new SelectList(materials, "Id", "Name", selectedMaterialId);
        }
    }
}