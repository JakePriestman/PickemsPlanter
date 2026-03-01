using PickemsPlanter.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PickemsPlanter.Pages;

public class LogoutModel(IEventTableService eventTableService, IUserPredictionsCachingService cachingService) : PageModel
{
	private List<SelectListItem> EventOptions { get; set; } = [];

	[BindProperty(SupportsGet = true)]
	public required string SteamId { get; init; }

	public async Task<IActionResult> OnGetAsync()
        {
		await LoadEventsAsync();

		foreach (var @event in EventOptions)
		{
			cachingService.EmptyUserCache(SteamId, @event.Value);
		}

		await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return Redirect("/Profile/Login");
	}

	private async Task LoadEventsAsync()
	{
		var events = await eventTableService.GetAllEventsAsync();

		EventOptions = [.. events.Select(x => new SelectListItem
		{
			Text = x.Name,
			Value = x.Id
		})];
	}
}
