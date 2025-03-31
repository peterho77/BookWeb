using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookWeb.Areas.Identity.Pages.Account
{
    public class EmailVerificationModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _sender;

        public string Email { get; set; }
        public string Code { get; set; }

        public EmailVerificationModel(UserManager<IdentityUser> userManager, IEmailSender sender)
        {
            _userManager = userManager;
            _sender = sender;
        }

        public async Task<IActionResult> OnGetAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Invalid request");
            }

            // Lưu email và code vào ViewModel để hiển thị trong giao diện
            Email = email;

            return Page();  // Trả về trang tương ứng
        }

        public async Task<IActionResult> OnPostAsync(string? code, string? email)
        {
            if (code == null || email == null)
            {
                return BadRequest("Invalid payload");
            }

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                return BadRequest("Invalid payload");
            }

            var isVerified = await _userManager.ConfirmEmailAsync(user, code);
            if (isVerified.Succeeded)
            {
                TempData["success"] = "You verified your email successfully";
                return RedirectToPage("/Account/Login");
            }

            return BadRequest("something went wrong");
        }
    }
}
