using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace assetWebApi.Pages.Company
{
    public class IndexModel : PageModel
    {
        [Display(Name = "User Role")]
        public int SelectedUserRoleId { get; set; }
        public IEnumerable<SelectListItem> UserRoles { get; set; }

        public void OnGet()
        {
            UserRoles = GetRolesFromStaticData();
        }

        private IEnumerable<SelectListItem> GetRolesFromStaticData()
        {
            // Replace this with your actual static data
            var roles = new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "Admin" },
                new SelectListItem { Value = "2", Text = "Manager" },
                new SelectListItem { Value = "3", Text = "Employee" },
                new SelectListItem { Value = "4", Text = "gfds" },
                new SelectListItem { Value = "5", Text = "Emplgvdsoyee" },
                new SelectListItem { Value = "6", Text = "Employeesa" },
                new SelectListItem { Value = "7", Text = "Employefde" },
                new SelectListItem { Value = "8", Text = "Employedsae" },
                new SelectListItem { Value = "9", Text = "zzzmployede" },
                new SelectListItem { Value = "10", Text = "zmployee" },
                new SelectListItem { Value = "11", Text = "dddmployee" },
                new SelectListItem { Value = "12", Text = "Employesdse" },
                new SelectListItem { Value = "13", Text = "Employee" },
                new SelectListItem { Value = "14", Text = "Employee" },
                new SelectListItem { Value = "15", Text = "Employee" },
                new SelectListItem { Value = "16", Text = "Employee" },
                new SelectListItem { Value = "17", Text = "Employee" },
                new SelectListItem { Value = "18", Text = "Employee" },
                new SelectListItem { Value = "19", Text = "Employee" },
                new SelectListItem { Value = "20", Text = "zzmployee" },
                new SelectListItem { Value = "21", Text = "Employee" },
                new SelectListItem { Value = "22", Text = "zzmployee" }
            };

            return roles;
        }
    }
}
