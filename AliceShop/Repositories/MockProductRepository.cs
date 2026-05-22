using AliceShop.Models;

namespace AliceShop.Repositories
{
    public class MockProductRepository : IProductRepository
    {
        private readonly List<Product> _products;

        public MockProductRepository()
        {
            // Khởi tạo dữ liệu mẫu chuẩn ngành Trang sức & Phụ kiện cho AliceShop
            _products = new List<Product>
            {
                new Product
                {
                    Id = 1,
                    Name = "Nhẫn Bạc Ý S925 Đính Đá Solitaire",
                    Price = 450000,
                    Description = "Nhẫn bạc cao cấp xi bạch kim, đính đá Cubic Zirconia lấp lánh sang trọng.",
                    ImageUrl = "https://images.unsplash.com/photo-1605100804763-247f67b3557e?w=500", // Link ảnh minh họa nhẫn
                    CategoryId = 1 // Giả định 1 là nhóm Nhẫn
                },
                new Product
                {
                    Id = 2,
                    Name = "Dây Chuyền Ngọc Trai Nhân Tạo Alice",
                    Price = 680000,
                    Description = "Dây chuyền dáng ngắn thanh lịch, phù hợp phối đồ công sở hoặc đi tiệc nhẹ.",
                    ImageUrl = "https://images.unsplash.com/photo-1599643478518-a784e5dc4c8f?w=500", // Link ảnh minh họa dây chuyền
                    CategoryId = 2 // Giả định 2 là nhóm Dây chuyền
                },
                new Product
                {
                    Id = 3,
                    Name = "Vòng Tay Kim Loại Khắc Họa Tiết Retro",
                    Price = 180000,
                    Description = "Phụ kiện vòng tay phong cách cá tính, chất liệu titan không gỉ.",
                    ImageUrl = "https://images.unsplash.com/photo-1611591437281-460bfbe1220a?w=500", // Link ảnh minh họa vòng tay
                    CategoryId = 3 // Giả định 3 là nhóm Vòng tay
                }
            };
        }

        public IEnumerable<Product> GetAll()
        {
            return _products;
        }

        public Product GetById(int id)
        {
            return _products.FirstOrDefault(p => p.Id == id);
        }

        public void Add(Product product)
        {
            // Sửa lỗi Crash: Kiểm tra nếu danh sách trống thì gán ID bắt đầu từ 1, ngược lại lấy Max + 1
            product.Id = _products.Any() ? _products.Max(p => p.Id) + 1 : 1;
            _products.Add(product);
        }

        public void Update(Product product)
        {
            var index = _products.FindIndex(p => p.Id == product.Id);
            if (index != -1)
            {
                _products[index] = product;
            }
        }

        public void Delete(int id)
        {
            var product = _products.FirstOrDefault(p => p.Id == id);
            if (product != null)
            {
                _products.Remove(product);
            }
        }
    }
}