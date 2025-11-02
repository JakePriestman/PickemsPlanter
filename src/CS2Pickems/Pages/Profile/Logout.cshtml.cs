using CS2Pickems.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CS2Pickems.Pages
{
    public class LogoutModel(List<SelectListItem> eventOptions, IUserPredictionsCachingService cachingService) : PageModel
    {
		private List<SelectListItem> EventOptions { get; set; } = eventOptions;

		[BindProperty(SupportsGet = true)]
		public required string SteamId { get; init; }

		public async Task<IActionResult> OnGet()
        {
			foreach (var @event in EventOptions)
			{
				cachingService.EmptyUserCache(SteamId, @event.Value);
			}

			await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return Redirect("/Profile/Login");
		}
    }
}
