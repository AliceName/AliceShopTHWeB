# 💎 AliceShop - Hệ Thống Quản Lý & Kinh Doanh Trang Sức Cao Cấp

AliceShop là một ứng dụng web thương mại điện tử chuyên biệt dành cho ngành hàng vàng bạc, đá quý và phụ kiện trang sức cao cấp. Dự án được xây dựng trên nền tảng **ASP.NET Core MVC** theo kiến trúc phân tầng, kết hợp mô hình **Repository Pattern** và nguyên lý **Dependency Injection (DI)** giúp hệ thống vận hành mượt mà, dễ dàng bảo trì và mở rộng.

---

## ✨ Tính Năng Nổi Bật

### 🧑‍ quản trị viên (Admin Dashboard)
* **Quản lý sản phẩm (CRUD):** Giao diện bảng (List) gọn gàng, hỗ trợ xem chi tiết, thêm mới, chỉnh sửa thông tin và xóa bỏ sản phẩm.
* **Xử lý hình ảnh nâng cao:** Tách biệt ảnh đại diện chính (`ImageUrl`) và album ảnh phụ chi tiết (`ImageUrls`), tự động bóc tách và chuẩn hóa tên file bằng chuỗi mã hóa định danh `Guid` sạch tiếng Việt.
* **Quản lý danh mục:** Phân loại nhóm trang sức (Nhẫn, Dây chuyền, Bông tai...) với cơ chế tự động tăng mã ID an toàn trên bộ nhớ RAM tạm thời (`In-Memory Singleton`).

### 👩‍💼 Khách hàng (Storefront)
* **Trang chủ (Luxury Theme):** Banner Hero Section thiết kế tông màu Đen - Vàng Kim sang trọng, hiển thị danh mục nổi bật và các cam kết dịch vụ tín nhiệm.
* **Bộ sưu tập dạng Lưới (Grid Cards):** Tự động co giãn tương thích đa màn hình (Responsive Grid), tích hợp hiệu ứng chuyển động nhấc thẻ (`Hover Translate`) và phóng to hình ảnh mượt mà.
* **Xem Gallery đa ảnh:** Trang chi tiết sản phẩm cho phép click chuyển đổi xem nhiều góc chụp của sản phẩm qua các ô ảnh nhỏ (Thumbnails) đồng bộ với ảnh tiêu điểm chính.

---

## 💻 Công Nghệ Sử Dụng

* **Backend Core:** .NET 8.0 / ASP.NET Core MVC
* **Frontend UI:** Bootstrap 5.3 (Luxury Dark & Gold Theme), Bootstrap Icons
* **Kiến trúc mã nguồn:** Repository Pattern (MockData), Dependency Injection Lifecycle (Singleton)
* **Ngôn ngữ lập trình:** C#, HTML5, CSS3, JavaScript (ES6+)

---

## 📁 Cấu Trúc Thư Mục Dự Án Chính

```text
AliceShop/
│
├── Controllers/
│   ├── HomeController.cs        # Điều hướng Trang chủ, Liên hệ
│   ├── ProductController.cs     # Xử lý CRUD sản phẩm (Admin List & Client Grid)
│   └── CategoryController.cs    # Xử lý CRUD danh mục phân loại
│
├── Models/
│   ├── Product.cs               # Thực thể sản phẩm (Id, Name, Price, ImageUrls...)
│   └── Category.cs              # Thực thể danh mục (Id, Name)
│
├── Repositories/
│   ├── IProductRepository.cs / MockProductRepository.cs
│   └── ICategoryRepository.cs / MockCategoryRepository.cs
│
├── Views/
│   ├── Home/                    # View Trang chủ (Index.cshtml)
│   ├── Product/                 # Giao diện quản trị List & mua sắm Grid Cards
│   ├── Category/                # Giao diện quản lý phân loại nhóm
│   └── Shared/_Layout.cshtml    # Khung giao diện chung (Navbar, Footer đồng bộ)
│
└── wwwroot/                     # Tài nguyên tĩnh (CSS, JS, hình ảnh sản phẩm tải lên)
