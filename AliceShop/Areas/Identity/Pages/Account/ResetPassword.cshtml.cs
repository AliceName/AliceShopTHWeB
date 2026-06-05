// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using AliceShop.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace AliceShop.Areas.Identity.Pages.Account
{
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ResetPasswordModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Email không được để trống.")]
            [EmailAddress(ErrorMessage = "Định dạng Email không hợp lệ.")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Mật khẩu không được để trống.")]
            [StringLength(100, ErrorMessage = "Mật khẩu phải dài từ {2} đến {1} ký tự.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "Mật khẩu nhập lại không trùng khớp.")]
            public string ConfirmPassword { get; set; }

            [Required]
            public string Code { get; set; }
        }

        // 🌟 SỬA TẠI ĐÂY: Thêm tham số string email = null vào hàm OnGet
        public IActionResult OnGet(string code = null, string email = null)
        {
            if (code == null)
            {
                return BadRequest("Mã xác thực (Token) không hợp lệ hoặc đã hết hạn.");
            }
            else
            {
                Input = new InputModel
                {
                    Code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code)),
                    Email = email // 🔥 BẮT BUỘC: Nạp Email từ URL vào form để OnPost có dữ liệu tìm User
                };
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                // Để bảo mật không lộ email, vẫn chuyển hướng, nhưng chúng ta in log ra để kiểm tra
                System.Diagnostics.Debug.WriteLine($"[TEST LOG] Không tìm thấy người dùng có Email: {Input.Email}");
                return RedirectToPage("./ResetPasswordConfirmation");
            }

            var result = await _userManager.ResetPasswordAsync(user, Input.Code, Input.Password);
            if (result.Succeeded)
            {
                System.Diagnostics.Debug.WriteLine($"[TEST LOG] ĐỔI MẬT KHẨU THÀNH CÔNG CHO: {Input.Email}");
                return RedirectToPage("./ResetPasswordConfirmation");
            }

            // Nếu thất bại (Ví dụ: Password không đủ độ phức tạp), in chi tiết lỗi ra Output
            System.Diagnostics.Debug.WriteLine("============= LỖI HỆ THỐNG IDENTITY =============");
            foreach (var error in result.Errors)
            {
                System.Diagnostics.Debug.WriteLine($"MÃ LỖI: {error.Code} - CHI TIẾT: {error.Description}");
                ModelState.AddModelError(string.Empty, error.Description);
            }
            System.Diagnostics.Debug.WriteLine("=================================================");

            return Page();
        }
    }
}