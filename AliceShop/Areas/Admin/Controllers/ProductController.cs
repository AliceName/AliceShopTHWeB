using AliceShop.Models;
using AliceShop.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System;

namespace AliceShop.Areas.Admin.Controllers
{
    [Area("Admin")] // 🔥 Xác định định danh thuộc vùng quản trị Admin
    [Authorize(Roles = "Admin")] // 🔥 Khóa an toàn: Chỉ tài khoản nhóm Admin mới được chạm vào
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

        // ── 1. Index: Bảng quản lý dữ liệu sản phẩm của Admin ──
        public async Task<IActionResult> Index()
        {
            var products = await _productRepository.GetAllAsync();
            ViewBag.CategoriesList = await _categoryRepository.GetAllAsync();
            return View(products);
        }
        // ── 1c. Details: Giao diện xem chi tiết thông số sản phẩm nội bộ của Admin ──
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            // Lấy thông tin sản phẩm từ Repository bao gồm cả danh sách ảnh phụ con
            var product = await _productRepository.GetByIdAsync(id.Value);
            if (product == null) return NotFound();

            return View(product); // Hệ thống sẽ tìm tệp Areas/Admin/Views/Product/Details.cshtml
        }

        // ── 2. Create (GET) ──
        public async Task<IActionResult> Create()
        {
            await LoadDropdownsAsync();
            return View();
        }

        // ── 2. Create (POST) ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Price,Description,CategoryId,MaterialId")] Product product, IFormFile? imageUrl, List<IFormFile>? imageFiles)
        {
            ModelState.Remove("ImageUrl");
            ModelState.Remove("Images");
            ModelState.Remove("Category");
            ModelState.Remove("Material");

            if (ModelState.IsValid)
            {
                product.Images = new List<ProductImage>();

                if (imageUrl != null && imageUrl.Length > 0)
                {
                    product.ImageUrl = await SaveImage(imageUrl);
                }
                else
                {
                    product.ImageUrl = "/images/placeholder.png";
                }

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

        // ── 3. Edit (GET) ──
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();

            await LoadDropdownsAsync(product.CategoryId, product.MaterialId);
            return View(product);
        }

        // ── 3. Edit (POST) ──
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

                if (imageFiles != null && imageFiles.Count > 0)
                {
                    existing.Images ??= new List<ProductImage>();
                    foreach (var file in imageFiles.Where(f => f.Length > 0))
                    {
                        var savedPath = await SaveImage(file);
                        existing.ImageUrl = savedPath;
                        existing.Images.Add(new ProductImage { Url = savedPath, ProductId = existing.Id });
                    }
                }

                await _productRepository.UpdateAsync(existing);
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdownsAsync(product.CategoryId, product.MaterialId);
            return View(product);
        }

        // ── 4. Delete (GET) ──
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        // ── 4. Delete (POST) ──
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

        // ── Các hàm hỗ trợ nạp dữ liệu ảnh và dropdown ──
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