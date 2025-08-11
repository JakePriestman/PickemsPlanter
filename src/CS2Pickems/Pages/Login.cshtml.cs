using CS2Pickems.APIs;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CS2Pickems.Pages
{
    public class LoginModel(ILoginAPI loginAPI) : PageModel
    {
        private readonly ILoginAPI _loginAPI = loginAPI;

        public IActionResult OnPostLogin()
        {
            if (User.Identity?.IsAuthenticated is true)
                return RedirectToPage("PickEms/ChooseEvent");

			string returnUrl = Request.GetEncodedUrl().Replace("Login", "SteamCallback");
			string realm = $"{Request.Scheme}://{Request.Host}";

            return Redirect(_loginAPI.Login(returnUrl, realm));
		}
    }
}
