using AliceShop.Models;
using Microsoft.EntityFrameworkCore;

namespace AliceShop.Repositories
{
    public class EFProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _context;

        public EFProductRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // ── 1. Lấy toàn bộ danh sách sản phẩm (Đã đồng bộ nạp kèm tất cả bảng liên quan) ──
        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await _context.Products
                .Include(p => p.Category)  // Nạp kèm danh mục để hiện nhãn phân loại
                .Include(p => p.Material)  // Nạp kèm chất liệu động từ SQL
                .Include(p => p.Images)    // 🔥 SỬA TẠI ĐÂY: Nạp album ảnh phụ để trang Grid/Card không bị mất hình
                .AsNoTracking()            // Tăng tốc độ truy vấn đọc dữ liệu (Bypass tracking)
                .ToListAsync();
        }

        // ── 2. Lấy chi tiết một sản phẩm theo mã Id (Đã hoàn hảo) ──────────────────────
        public async Task<Product?> GetByIdAsync(int id)
        {
            return await _context.Products
                .Include(p => p.Category)   // Nạp thông tin danh mục cha
                .Include(p => p.Material)   // Nạp thông tin chất liệu chế tác
                .Include(p => p.Images)     // Nạp bộ sưu tập ảnh phụ con
                                            // 🔥 NẠP LIÊN KẾT BIẾN THỂ SIZE VÀ TÊN SIZE ĐI KÈM
                .Include(p => p.ProductSizeVariants)
                    .ThenInclude(pv => pv.ProductSize)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        // ── 3. Thêm mới một sản phẩm ───────────────────────────────────────────────────
        public async Task AddAsync(Product product)
        {
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();
        }

        // ── 4. Chỉnh sửa cập nhật thông tin sản phẩm ─────────────────────────────────────
        public async Task UpdateAsync(Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }

        // ── 5. Xóa bỏ sản phẩm ra khỏi hệ thống (Đã vá lỗi chặn sập phần mềm) ────────────
        public async Task DeleteAsync(int id)
        {
            // Nạp sản phẩm kèm theo bảng con Images để EF tự động kích hoạt xóa Cascade (Xóa sạch bản ghi con)
            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            // Kiểm tra an toàn: Chỉ ra lệnh xóa khi thực thể thực sự tồn tại trong SQL Server
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
        }
        // 1. Thêm định nghĩa này vào IProductRepository.cs nếu có sử dụng Interface:
        // Task<IEnumerable<Product>> SearchByNameAsync(string keyword);

        // 2. Thêm hàm thực thi này vào file Repositories/EFProductRepository.cs:
        public async Task<IEnumerable<Product>> SearchByNameAsync(string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                return await GetAllAsync();
            }

            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Material)
                .Include(p => p.Images)
                .Where(p => p.Name.Contains(keyword)) // Lọc theo từ khóa (EF Core tự dịch sang lệnh LIKE trong SQL)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}