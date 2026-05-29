using AliceShop.Models;

namespace AliceShop.Repositories
{
    public interface IMaterialRepository
    {
        // Lấy toàn bộ danh sách chất liệu từ SQL Server lên
        Task<IEnumerable<Material>> GetAllAsync();

        // Tìm một chất liệu duy nhất bằng mã định danh Id
        Task<Material?> GetByIdAsync(int id);

        // Thêm mới một chất liệu vào hệ thống
        Task AddAsync(Material material);

        // Cập nhật thông tin sửa đổi của chất liệu
        Task UpdateAsync(Material material);

        // Xóa hoàn toàn một chất liệu ra khỏi Database
        Task DeleteAsync(int id);
    }
}
