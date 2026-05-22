using AliceShop.Models;

namespace AliceShop.Repositories
{
    public class MockCategoryRepository : ICategoryRepository
    {
        private readonly List<Category> _categoryList;

        public MockCategoryRepository()
        {
            // Khởi tạo danh mục dữ liệu mẫu chuẩn cho AliceShop
            _categoryList = new List<Category>
            {
                new Category { Id = 1, Name = "Nhẫn" },
                new Category { Id = 2, Name = "Dây Chuyền / Vòng Cổ" },
                new Category { Id = 3, Name = "Vòng Tay / Lắc Tay" },
                new Category { Id = 4, Name = "Bông Tai" },
                new Category { Id = 5, Name = "Vòng Chân" }
            };
        }

        // Lấy toàn bộ danh sách danh mục
        public IEnumerable<Category> GetAllCategories()
        {
            return _categoryList;
        }

        // Tìm danh mục theo ID
        public Category GetById(int id)
        {
            return _categoryList.FirstOrDefault(c => c.Id == id);
        }

        // Thêm danh mục mới (Có cơ chế tự tăng ID an toàn)
        public void Add(Category category)
        {
            // Nếu danh sách trống thì ID bắt đầu từ 1, ngược lại lấy Max ID + 1
            category.Id = _categoryList.Any() ? _categoryList.Max(c => c.Id) + 1 : 1;
            _categoryList.Add(category);
        }

        // Cập nhật thông tin danh mục
        public void Update(Category category)
        {
            var index = _categoryList.FindIndex(c => c.Id == category.Id);
            if (index != -1)
            {
                _categoryList[index] = category;
            }
        }

        // Xóa danh mục dựa trên ID
        public void Delete(int id)
        {
            var category = _categoryList.FirstOrDefault(c => c.Id == id);
            if (category != null)
            {
                _categoryList.Remove(category);
            }
        }
    }
}