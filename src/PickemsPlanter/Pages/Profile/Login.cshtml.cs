using PickemsPlanter.APIs;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace PickemsPlanter.Pages;

[EnableRateLimiting("Auth")]
public class LoginModel(ILoginAPI loginAPI) : PageModel
{
        private readonly ILoginAPI _loginAPI = loginAPI;

	public IActionResult OnGet()
	{
		if (User.Identity?.IsAuthenticated is true)
			return RedirectToPage("/Profile/Overview");

		return Page();
	}

	public IActionResult OnPostLogin()
        {
            if (User.Identity?.IsAuthenticated is true)
                return RedirectToPage("/Profile/Overview");

		string returnUrl = Request.GetEncodedUrl().Replace("Login", "SteamCallback");
		string realm = $"{Request.Scheme}://{Request.Host}";

            return Redirect(_loginAPI.Login(returnUrl, realm));
	}
}
