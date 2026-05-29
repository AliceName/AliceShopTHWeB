using AliceShop.Models;
using Microsoft.EntityFrameworkCore;

namespace AliceShop.Repositories
{
    public class EFMaterialRepository : IMaterialRepository
    {
        private readonly ApplicationDbContext _context; // Thay bằng tên DbContext thực tế của bạn nếu khác

        public EFMaterialRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Thực thi lấy toàn bộ danh sách chất liệu
        public async Task<IEnumerable<Material>> GetAllAsync()
        {
            return await _context.Materials
                .OrderBy(m => m.Name) // Sắp xếp theo tên chữ từ A-Z cho đẹp dropdown
                .ToListAsync();
        }

        // 2. Thực thi tìm kiếm chất liệu theo mã Id
        public async Task<Material?> GetByIdAsync(int id)
        {
            return await _context.Materials
                .Include(m => m.Products) // Gộp kèm thông tin các sản phẩm thuộc chất liệu này nếu cần
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        // 3. Thực thi thêm mới chất liệu
        public async Task AddAsync(Material material)
        {
            await _context.Materials.AddAsync(material);
            await _context.SaveChangesAsync(); // Lưu vĩnh viễn xuống SQL Server
        }

        // 4. Thực thi cập nhật chất liệu
        public async Task UpdateAsync(Material material)
        {
            _context.Entry(material).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        // 5. Thực thi xóa chất liệu dựa trên Id
        public async Task DeleteAsync(int id)
        {
            var material = await _context.Materials.FindAsync(id);
            if (material != null)
            {
                _context.Materials.Remove(material);
                await _context.SaveChangesAsync();
            }
        }
    }
}
