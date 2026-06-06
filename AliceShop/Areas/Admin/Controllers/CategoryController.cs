using AliceShop.Models;
using AliceShop.Repositories;
using Microsoft.AspNetCore.Mvc;
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
    public class CategoryController : Controller
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IProductRepository _productRepository;
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext _context; // 🔥 Tiêm bối cảnh cơ sở dữ liệu để quản lý kích cỡ động

        public CategoryController(
            ICategoryRepository categoryRepository,
            IProductRepository productRepository,
            IWebHostEnvironment env,
            ApplicationDbContext context) // 🔥 Nhận DbContext từ Constructor
        {
            _categoryRepository = categoryRepository;
            _productRepository = productRepository;
            _env = env;
            _context = context;
        }

        // ── 1. Index: Giao diện danh sách danh mục của Admin ──
        public async Task<IActionResult> Index()
        {
            var categories = await _categoryRepository.GetAllAsync();
            return View(categories);
        }

        // ── 2. Create (GET) ──
        public IActionResult Create()
        {
            return View();
        }

        // ── 2. Create (POST) ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category, List<string> categorySizes)
        {
            ModelState.Remove("Products");

            if (ModelState.IsValid)
            {
                // 1. Lưu danh mục gốc vào CSDL trước thông qua Repository để sinh Id tự động
                await _categoryRepository.AddAsync(category);

                // 2. 🔥 LOGIC MỚI: Lưu bộ khung kích cỡ đi kèm danh mục
                if (categorySizes != null && categorySizes.Any())
                {
                    foreach (var sizeName in categorySizes)
                    {
                        if (!string.IsNullOrWhiteSpace(sizeName))
                        {
                            if (sizeName.Length > 100)
                            {
                                ModelState.AddModelError("", $"Kích cỡ '{sizeName.Substring(0, Math.Min(30, sizeName.Length))}...' quá dài. Tối đa 100 ký tự.");
                                return View(category);
                            }

                            var newSize = new ProductSize
                            {
                                SizeName = sizeName.Trim(),
                                CategoryId = category.Id // Gán mã ID danh mục vừa sinh ra ở trên
                            };
                            _context.ProductSizes.Add(newSize);
                        }
                    }
                    await _context.SaveChangesAsync(); // Đồng bộ lưu xuống SQL Server
                }

                return RedirectToAction(nameof(Index));
            }

            return View(category);
        }

        // ── 3. Details ──
        public async Task<IActionResult> Details(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null) return NotFound();

            // Nạp kèm danh sách size hiện tại của danh mục này ra trang xem chi tiết
            ViewBag.CategorySizes = await _context.ProductSizes
                .Where(ps => ps.CategoryId == id)
                .ToListAsync();

            return View(category);
        }

        // ── 4. Edit (GET) ──
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null) return NotFound();

            // 🔥 Nạp danh sách các kích cỡ cũ đang có của danh mục này để truyền lên giao diện chỉnh sửa
            ViewBag.CategorySizes = await _context.ProductSizes
                .Where(ps => ps.CategoryId == id)
                .Select(ps => ps.SizeName)
                .ToListAsync();

            return View(category);
        }

        // ── 4. Edit (POST) ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category category, List<string> categorySizes)
        {
            if (id != category.Id) return NotFound();

            ModelState.Remove("Products");

            if (ModelState.IsValid)
            {
                var existing = await _categoryRepository.GetByIdAsync(id);
                if (existing == null) return NotFound();

                // Cập nhật tên danh mục
                existing.Name = category.Name;
                await _categoryRepository.UpdateAsync(existing);

                // 🔥 LOGIC MỚI: Đồng bộ hóa lại danh sách Size của danh mục (Xóa cũ nạp lại mới)
                // 1. Tìm các size cũ
                var oldSizes = await _context.ProductSizes.Where(ps => ps.CategoryId == id).ToListAsync();

                // 2. Tìm danh sách các size ID đang bị dính với biến thể sản phẩm để tránh xóa nhầm lỗi khóa ngoại
                var sizesInUseIds = await _context.ProductSizeVariants.Select(pv => pv.ProductSizeId).Distinct().ToListAsync();

                foreach (var oldSize in oldSizes)
                {
                    // Nếu size cũ này chưa được gán cho sản phẩm nào, ta có thể làm sạch an toàn
                    if (!sizesInUseIds.Contains(oldSize.Id))
                    {
                        _context.ProductSizes.Remove(oldSize);
                    }
                }

                // 3. Nạp tập hợp size mới cập nhật từ giao diện Form gửi lên
                if (categorySizes != null && categorySizes.Any())
                {
                    foreach (var sizeName in categorySizes)
                    {
                        if (!string.IsNullOrWhiteSpace(sizeName))
                        {
                            if (sizeName.Length > 100)
                            {
                                ModelState.AddModelError("", $"Kích cỡ '{sizeName.Substring(0, Math.Min(30, sizeName.Length))}...' quá dài. Tối đa 100 ký tự.");
                                return View(category);
                            }

                            // Kiểm tra xem size này đã tồn tại sẵn trong danh mục chưa để tránh trùng lặp dữ liệu
                            var isExist = oldSizes.Any(os => string.Equals(os.SizeName.Trim(), sizeName.Trim(), StringComparison.OrdinalIgnoreCase));
                            if (!isExist)
                            {
                                var newSize = new ProductSize
                                {
                                    SizeName = sizeName.Trim(),
                                    CategoryId = id
                                };
                                _context.ProductSizes.Add(newSize);
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(category);
        }

        // ── 5. Delete (GET) ──
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null) return NotFound();

            var allProducts = await _productRepository.GetAllAsync();
            var productsInCategory = allProducts.Where(p => p.CategoryId == id).ToList();
            ViewBag.ProductCount = productsInCategory.Count;

            return View(category);
        }

        // ── 5. Delete (POST) ──
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var allProducts = await _productRepository.GetAllAsync();
            var productsInCategory = allProducts.Where(p => p.CategoryId == id).ToList();

            // 1. Dọn dẹp kho ảnh vật lý và sản phẩm liên quan
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

                // Xóa các cấu hình kho liên quan đến sản phẩm này trước
                var relatedVariants = _context.ProductSizeVariants.Where(pv => pv.ProductId == p.Id);
                _context.ProductSizeVariants.RemoveRange(relatedVariants);

                await _productRepository.DeleteAsync(p.Id);
            }

            // 2. 🔥 DỌN DẸP SẠCH: Xóa toàn bộ bộ khung size đi kèm của danh mục này trước khi xóa danh mục cha
            var relatedSizes = _context.ProductSizes.Where(ps => ps.CategoryId == id);
            _context.ProductSizes.RemoveRange(relatedSizes);
            await _context.SaveChangesAsync();

            // 3. Tiến hành xóa gốc danh mục cha ra khỏi hệ thống qua Repository
            await _categoryRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}