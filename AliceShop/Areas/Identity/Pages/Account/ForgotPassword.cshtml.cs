// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using AliceShop.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace AliceShop.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ForgotPasswordModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                // 🔥 ĐÃ SỬA: Loại bỏ vế check IsEmailConfirmedAsync để tài khoản chưa kích hoạt vẫn test quên mật khẩu được
                if (user == null)
                {
                    // Giữ nguyên cơ chế bảo mật ẩn danh tính tài khoản
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                // Tạo mã Token reset mật khẩu
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                // Thiết lập đường dẫn khôi phục an toàn kèm tham số email (Đồng bộ với trang ResetPassword)
                var callbackUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity", code, email = Input.Email }, // Đính kèm email để bên kia hứng dữ liệu
                    protocol: Request.Scheme);

                // Thực hiện gửi Email thật qua hệ thống MailKit
                await _emailSender.SendEmailAsync(
                    Input.Email,
                    "AliceStore - Khoi phuc mat khau tai khoan",
                    $"Chao ban, vui long khoi phuc mat khau tai khoan AliceStore cua ban bang cach <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>bam vao day</a>.");

                // In log kiểm tra tiến trình gửi mail thành công trong Output
                System.Diagnostics.Debug.WriteLine($"[MAIL SUCCESS] Da goi ham gui link reset toi: {Input.Email}");

                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            return Page();
        }
    }
}
