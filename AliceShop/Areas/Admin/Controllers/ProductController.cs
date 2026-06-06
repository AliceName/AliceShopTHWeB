using AliceShop.Models;
using AliceShop.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace AliceShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IMaterialRepository _materialRepository;
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext _context;

        public ProductController(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            IMaterialRepository materialRepository,
            IWebHostEnvironment env,
            ApplicationDbContext context)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _materialRepository = materialRepository;
            _env = env;
            _context = context;
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

            var product = await _productRepository.GetByIdAsync(id.Value);
            if (product == null) return NotFound();

            ViewBag.ProductSizes = await _context.ProductSizeVariants
                .Include(pv => pv.ProductSize)
                .Where(pv => pv.ProductId == id.Value)
                .ToListAsync();

            return View(product);
        }

        // ── 2. Create (GET) ──
        public async Task<IActionResult> Create()
        {
            await LoadDropdownsAsync();
            await LoadAvailableSizesAsync();
            return View();
        }

        // ── 2. Create (POST) ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Price,Description,CategoryId,MaterialId")] Product product, IFormFile? imageUrl, List<IFormFile>? imageFiles, List<SizeVariantInputModel> sizeVariants)
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

                if (sizeVariants != null && sizeVariants.Any())
                {
                    foreach (var item in sizeVariants)
                    {
                        if (item.IsSelected)
                        {
                            var sizeVar = new ProductSizeVariant
                            {
                                ProductId = product.Id,
                                ProductSizeId = item.ProductSizeId,
                                Price = item.Price > 0 ? item.Price : product.Price,
                                StockQuantity = item.StockQuantity
                            };
                            _context.ProductSizeVariants.Add(sizeVar);
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }

            await LoadDropdownsAsync(product.CategoryId, product.MaterialId);
            await LoadAvailableSizesAsync();
            return View(product);
        }

        // ── 3. Edit (GET) ──
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();

            await LoadDropdownsAsync(product.CategoryId, product.MaterialId);
            await LoadAvailableSizesForEditAsync(id);
            return View(product);
        }

        // ── 3. Edit (POST) ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Price,Description,CategoryId,MaterialId")] Product product, IFormFile? imageUrl, List<IFormFile>? imageFiles, List<int>? removedImageIds, List<SizeVariantInputModel> sizeVariants)
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

                // Handle main image update
                if (imageUrl != null && imageUrl.Length > 0)
                {
                    var oldFilePath = Path.Combine(_env.WebRootPath, existing.ImageUrl?.TrimStart('/') ?? "");
                    if (!string.IsNullOrEmpty(existing.ImageUrl) && System.IO.File.Exists(oldFilePath) && existing.ImageUrl != "/images/placeholder.png")
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                    existing.ImageUrl = await SaveImage(imageUrl);
                }

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
                        existing.Images.Add(new ProductImage { Url = savedPath, ProductId = existing.Id });
                    }
                }

                await _productRepository.UpdateAsync(existing);

                var oldVariants = _context.ProductSizeVariants.Where(pv => pv.ProductId == id);
                _context.ProductSizeVariants.RemoveRange(oldVariants);

                if (sizeVariants != null && sizeVariants.Any())
                {
                    foreach (var item in sizeVariants)
                    {
                        if (item.IsSelected)
                        {
                            var sizeVar = new ProductSizeVariant
                            {
                                ProductId = id,
                                ProductSizeId = item.ProductSizeId,
                                Price = item.Price > 0 ? item.Price : existing.Price,
                                StockQuantity = item.StockQuantity
                            };
                            _context.ProductSizeVariants.Add(sizeVar);
                        }
                    }
                }
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            await LoadDropdownsAsync(product.CategoryId, product.MaterialId);
            await LoadAvailableSizesForEditAsync(id);
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

            var relatedSizes = _context.ProductSizeVariants.Where(pv => pv.ProductId == id);
            _context.ProductSizeVariants.RemoveRange(relatedSizes);
            await _context.SaveChangesAsync();

            await _productRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        // ── CÁC HÀM TRỢ GIÚP ĐƯỢC BỔ SUNG ĐỂ QUẢN LÝ SIZE ĐỘNG ──

        private async Task LoadAvailableSizesAsync()
        {
            var allSizes = await _context.ProductSizes.ToListAsync();
            // 🔥 ĐA SỬA: Gán thêm CategoryId từ bảng ProductSizes vào ô trung gian
            ViewBag.AvailableSizes = allSizes.Select(s => new SizeVariantInputModel
            {
                ProductSizeId = s.Id,
                SizeName = s.SizeName,
                Price = 0,
                StockQuantity = 0,
                IsSelected = false,
                CategoryId = s.CategoryId
            }).ToList();
        }

        private async Task LoadAvailableSizesForEditAsync(int productId)
        {
            var allSizes = await _context.ProductSizes.ToListAsync();
            var currentVariants = await _context.ProductSizeVariants
                .Where(pv => pv.ProductId == productId)
                .ToListAsync();

            // 🔥 ĐA SỬA: Gán thêm CategoryId từ bảng ProductSizes vào ô trung gian cho trang Edit
            ViewBag.AvailableSizes = allSizes.Select(s => {
                var match = currentVariants.FirstOrDefault(cv => cv.ProductSizeId == s.Id);
                return new SizeVariantInputModel
                {
                    ProductSizeId = s.Id,
                    SizeName = s.SizeName,
                    Price = match?.Price ?? 0,
                    StockQuantity = match?.StockQuantity ?? 0,
                    IsSelected = match != null,
                    CategoryId = s.CategoryId
                };
            }).ToList();
        }

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

    public class SizeVariantInputModel
    {
        public int ProductSizeId { get; set; }
        public string SizeName { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public bool IsSelected { get; set; }
        public int CategoryId { get; set; } // 🔥 ĐA SỬA: Thêm để hứng mã danh mục đồng bộ UI
    }
}