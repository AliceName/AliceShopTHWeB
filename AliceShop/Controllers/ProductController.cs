using AliceShop.Models;
using AliceShop.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

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

        // 1. Đồng bộ danh sách sản phẩm
        public IActionResult Index()
        {
            var products = _productRepository.GetAll();
            ViewBag.CategoriesList = _categoryRepository.GetAllCategories();
            return View(products);
        }

        // 1b. GIAO DIỆN BỘ SƯU TẬP (Dành cho Khách hàng) - Chuyển sang dạng GRID CARDS
        public IActionResult Collection()
        {
            var products = _productRepository.GetAll();
            ViewBag.CategoriesList = _categoryRepository.GetAllCategories();
            return View(products); // Sẽ nạp file Views/Product/Collection.cshtml (Dạng Grid)
        }

        // 2. Chi tiết sản phẩm
        public IActionResult Details(int id)
        {
            var product = _productRepository.GetById(id);
            if (product == null)
            {
                return NotFound();
            }
            ViewBag.CategoriesList = _categoryRepository.GetAllCategories();
            return View(product);
        }

        // 3. Giao diện thêm sản phẩm
        public IActionResult Create()
        {
            LoadCategoriesToViewBag();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Price,Description,CategoryId")] Product product, IFormFile imageUrl, List<IFormFile> imageUrls)
        {
            product.ImageUrls = new List<string>();

            // Xử lý lưu hình ảnh đại diện chính
            if (imageUrl != null && imageUrl.Length > 0)
            {
                product.ImageUrl = await SaveImage(imageUrl);
            }
            else
            {
                // Vá lỗi: Gán ảnh placeholder nếu Admin không chọn ảnh đại diện
                product.ImageUrl = "/images/placeholder.png";
            }

            // Xử lý lưu album ảnh phụ chi tiết
            if (imageUrls != null && imageUrls.Any(f => f.Length > 0))
            {
                foreach (var file in imageUrls)
                {
                    if (file.Length > 0)
                    {
                        product.ImageUrls.Add(await SaveImage(file));
                    }
                }
            }
            else
            {
                // Đồng bộ mảng phụ cũng có ít nhất 1 ảnh hiển thị ở trang Details
                product.ImageUrls.Add(product.ImageUrl);
            }
            // ════ 3. ĐỒNG BỘ: ĐẢM BẢO ẢNH CHÍNH LUÔN NẰM TRONG DANH SÁCH ẢNH PHỤ ════
            if (!string.IsNullOrEmpty(product.ImageUrl))
            {
                // Nếu trong danh sách ảnh phụ chưa có đường dẫn của ảnh chính hiện tại
                if (!product.ImageUrls.Contains(product.ImageUrl))
                {
                    // Chèn ảnh chính vào ngay vị trí đầu tiên (Index 0) của bộ sưu tập ảnh phụ
                    product.ImageUrls.Insert(0, product.ImageUrl);
                }
            }

            if (ModelState.IsValid)
            {
                _productRepository.Add(product);
                return RedirectToAction(nameof(Index));
            }

            LoadCategoriesToViewBag();
            return View(product);
        }

        // 4. Giao diện cập nhật sản phẩm
        public IActionResult Edit(int id)
        {
            var product = _productRepository.GetById(id);
            if (product == null)
            {
                return NotFound();
            }

            LoadCategoriesToViewBag();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Price,Description,CategoryId")] Product product, IFormFile imageUrl, List<IFormFile> imageUrls)
        {
            if (id != product.Id)
            {
                return BadRequest();
            }

            // 1. Lấy sản phẩm gốc từ RAM để chuẩn bị sao chép dữ liệu ảnh
            var existingProduct = _productRepository.GetById(id);
            if (existingProduct == null)
            {
                return NotFound();
            }

            // Khởi tạo danh sách mới tinh để bẻ gãy tham chiếu vùng nhớ cũ (Tránh lỗi gán đè RAM)
            product.ImageUrls = new List<string>();

            // 2. XỬ LÝ ẢNH ĐẠI DIỆN CHÍNH
            if (imageUrl != null && imageUrl.Length > 0)
            {
                product.ImageUrl = await SaveImage(imageUrl);
            }
            else
            {
                product.ImageUrl = existingProduct.ImageUrl;
            }

            // 3. XỬ LÝ ALBUM ẢNH PHỤ
            if (imageUrls != null && imageUrls.Any(f => f.Length > 0))
            {
                foreach (var file in imageUrls)
                {
                    if (file.Length > 0)
                    {
                        product.ImageUrls.Add(await SaveImage(file));
                    }
                }
            }
            else
            {
                // QUAN TRỌNG: Tạo bản sao danh sách mới (.ToList()) thay vì gán bằng trực tiếp để tránh lỗi tham chiếu RAM
                if (existingProduct.ImageUrls != null)
                {
                    product.ImageUrls = existingProduct.ImageUrls.ToList();
                }
            }
            // ════ 3. ĐỒNG BỘ: ĐẢM BẢO ẢNH CHÍNH LUÔN NẰM TRONG DANH SÁCH ẢNH PHỤ ════
            if (!string.IsNullOrEmpty(product.ImageUrl))
            {
                // Nếu trong danh sách ảnh phụ chưa có đường dẫn của ảnh chính hiện tại
                if (!product.ImageUrls.Contains(product.ImageUrl))
                {
                    // Chèn ảnh chính vào ngay vị trí đầu tiên (Index 0) của bộ sưu tập ảnh phụ
                    product.ImageUrls.Insert(0, product.ImageUrl);
                }
            }

            // 4. SỬA LỖI CHÍ MẠNG: Ép trình duyệt xóa bỏ kiểm tra hợp lệ của 2 trường ảnh tĩnh
            // Điều này đảm bảo ModelState luôn luôn hợp lệ (True) sau khi ta đã gán dữ liệu thủ công ở trên
            ModelState.Remove("imageUrl");
            ModelState.Remove("imageUrls");
            ModelState.Remove("ImageUrl");
            ModelState.Remove("ImageUrls");

            // 5. TIẾN HÀNH LƯU THAY ĐỔI
            if (ModelState.IsValid)
            {
                _productRepository.Update(product);
                return RedirectToAction(nameof(Index));
            }

            // Nếu vẫn có lỗi giao diện khác (như trống tên), nạp lại danh mục để hiện form
            LoadCategoriesToViewBag();
            return View(product);
        }
        // Hàm bổ trợ lưu ảnh tập trung chuẩn mã hóa Guid
        private async Task<string> SaveImage(IFormFile image)
        {
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var fileExtension = Path.GetExtension(image.FileName);
            var fileName = Guid.NewGuid().ToString() + fileExtension;
            var filePath = Path.Combine(folderPath, fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }

            return "/images/products/" + fileName;
        }

        // 5. Giao diện xác nhận xóa
        public IActionResult Delete(int id)
        {
            var product = _productRepository.GetById(id);
            if (product == null)
            {
                return NotFound();
            }
            ViewBag.CategoriesList = _categoryRepository.GetAllCategories();
            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var product = _productRepository.GetById(id);
            if (product != null)
            {
                _productRepository.Delete(id);
            }
            return RedirectToAction(nameof(Index));
        }

        private void LoadCategoriesToViewBag()
        {
            var categories = _categoryRepository.GetAllCategories();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
        }
    }
}