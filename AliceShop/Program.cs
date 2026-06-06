using AliceShop.Models;
using AliceShop.Repositories;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;

var builder = WebApplication.CreateBuilder(args);

// ── 1. ĐĂNG KÝ CƠ SỞ DỮ LIỆU SQL SERVER ───────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── 2. ĐĂNG KÝ HỆ THỐNG DANH TÍNH IDENTITY ────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // 1. Cấu hình xác thực tài khoản (Đã làm ở bước trước)
        options.SignIn.RequireConfirmedAccount = false;
        options.SignIn.RequireConfirmedEmail = false;

        // 2. 🔥 CẤU HÌNH NỚI LỎNG CHÍNH SÁCH MẬT KHẨU ĐỂ TEST CHO DỄ:
        options.Password.RequireDigit = false;             // Không bắt buộc phải có chữ số
        options.Password.RequireLowercase = false;         // Không bắt buộc phải có chữ thường
        options.Password.RequireNonAlphanumeric = false;   // Không bắt buộc phải có ký tự đặc biệt (@,#,!)
        options.Password.RequireUppercase = false;         // Không bắt buộc phải có chữ hoa
        options.Password.RequiredLength = 6;               // Độ dài tối thiểu chỉ cần 6 ký tự bất kỳ
        options.Password.RequiredUniqueChars = 1;          // Số ký tự khác nhau tối thiểu
    })
    .AddDefaultTokenProviders()
    .AddDefaultUI()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// 2. 🔥 CẤU HÌNH COOKIE GHI NHỚ:
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";

    // Cấu hình thời gian ghi nhớ đăng nhập (Ví dụ: 14 ngày)
    options.ExpireTimeSpan = TimeSpan.FromDays(14);

    // Nếu người dùng liên tục truy cập trong 14 ngày này, Cookie sẽ tự động gia hạn thêm
    options.SlidingExpiration = true;
});

// ── 3. ĐĂNG KÝ DỊCH VỤ GỬI MAIL THẬT (Đã sửa đổi tối ưu) ───────────────────
builder.Services.AddSingleton<IEmailSender, RealEmailSender>();

// ── 4. ĐĂNG KÝ CÁC DỊCH VỤ CONTROLLER & REPOSITORY ────────────────────────
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddScoped<IProductRepository, EFProductRepository>();
builder.Services.AddScoped<ICategoryRepository, EFCategoryRepository>();
builder.Services.AddScoped<IMaterialRepository, EFMaterialRepository>();


builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Giỏ hàng lưu tạm trong 30 phút
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});




var app = builder.Build();

// ── 5. CẤU HÌNH PIPELINE XỬ LÝ HTTP (MIDDLEWARE) ─────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();


app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// ── 6. ĐỊNH TUYẾN ROUTING HỆ THỐNG TRANG ──────────────────────────────────
app.MapRazorPages();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// ── 7. LỚP GỬI EMAIL THẬT CHUẨN ĐỒNG BỘ ───────────────────────────────────
// Đổi tên thành RealEmailSender cho đúng ngữ nghĩa gửi mail thật
public class RealEmailSender : IEmailSender
{
    // Đã xóa bỏ hoàn toàn Constructor chứa IConfiguration bị lỗi vòng lặp lifecycle

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("AliceStore Support", "tablinh2020@gmail.com"));
        message.To.Add(new MailboxAddress("", email));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = htmlMessage };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync("tablinh2020@gmail.com", "seztzbxynbfjvoji");
            await client.SendAsync(message);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CRITICAL] LỖI GỬI EMAIL: {ex.Message}");
            throw;
        }
        finally
        {
            await client.DisconnectAsync(true);
        }
    }
}