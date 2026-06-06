using AliceShop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AliceShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            var userList = await _userManager.Users.ToListAsync();
            var userRoleVMList = new List<UserVM>();

            foreach (var user in userList)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoleVMList.Add(new UserVM
                {
                    User = user,
                    Role = roles.FirstOrDefault() ?? "None"
                });
            }

            return View(userRoleVMList);
        }

        [HttpPost]
        public async Task<IActionResult> LockUnlock([FromBody] string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "Lỗi khi thực hiện thao tác" });
            }

            if (user.LockoutEnd != null && user.LockoutEnd > DateTime.Now)
            {
                // Mở khóa (Unlock)
                user.LockoutEnd = DateTime.Now;
            }
            else
            {
                // Khóa (Lock) - 1000 năm
                user.LockoutEnd = DateTime.Now.AddYears(1000);
            }
            
            await _userManager.UpdateAsync(user);

            return Json(new { success = true, message = "Thao tác thành công" });
        }

        [HttpGet]
        public async Task<IActionResult> EditRole(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _roleManager.Roles.Select(x => x.Name).ToListAsync();
            var userRoles = await _userManager.GetRolesAsync(user);

            var vm = new UserVM
            {
                User = user,
                Role = userRoles.FirstOrDefault() ?? "",
                RoleList = roles.Select(i => new SelectListItem
                {
                    Text = i,
                    Value = i
                })
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRole(UserVM vm)
        {
            var user = await _userManager.FindByIdAsync(vm.User.Id);
            if (user == null)
            {
                return NotFound();
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            
            // Xóa tất cả các role hiện tại
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            
            // Thêm role mới
            if (!string.IsNullOrEmpty(vm.Role))
            {
                await _userManager.AddToRoleAsync(user, vm.Role);
            }

            TempData["success"] = "Đã cập nhật vai trò người dùng thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
