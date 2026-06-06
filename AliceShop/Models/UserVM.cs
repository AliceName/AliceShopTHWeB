using Microsoft.AspNetCore.Mvc.Rendering;

namespace AliceShop.Models
{
    public class UserVM
    {
        public ApplicationUser User { get; set; }
        public string Role { get; set; }
        public IEnumerable<SelectListItem>? RoleList { get; set; }
    }
}
