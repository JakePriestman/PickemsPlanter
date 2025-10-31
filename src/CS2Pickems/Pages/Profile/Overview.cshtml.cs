using CS2Pickems.Models;
using CS2Pickems.Models.TableStorage;
using CS2Pickems.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;
using System.Net;
using System.Security.Claims;

namespace CS2Pickems.Pages.Profile
{
	public class OverviewModel(ITableStorageService tableStorageService, IUserPredictionsCachingService cachingService, IMemoryCache memoryCache, List<SelectListItem> eventOptions, IHttpContextAccessor httpContextAccessor) : PageModel
	{
		[BindProperty]
		public string SelectedEvent { get; set; } = string.Empty;

		public required string? PersonaName = httpContextAccessor?.HttpContext?.User.FindFirst("PersonaName")?.Value;

		public required string? Avatar = httpContextAccessor?.HttpContext?.User.FindFirst("Avatar")?.Value;

		public required string SteamId = httpContextAccessor?.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;

		public List<SelectListItem> EventOptions { get; set; } = eventOptions;

		[BindProperty]
		public Dictionary<string, string> AuthCodes { get; set; } = eventOptions.ToDictionary(key => key.Value, value => string.Empty);

		private const string FAKE_AUTH_CODE = "FAKE_AUTH_CODE";

		public async Task OnGetAsync()
		{
			foreach (var key in AuthCodes.Keys)
			{
				bool tableEntityExists = await tableStorageService.ExistsAsync(SteamId, key);

				if (tableEntityExists)
					AuthCodes[key] = FAKE_AUTH_CODE;
			}
		}

		public async Task<IActionResult> OnPostChooseEvent()
		{

			var eventName = EventOptions.First(x => x.Value == SelectedEvent).Text;

			var encodedName = WebUtility.UrlEncode(eventName);

			var authCode = AuthCodes[SelectedEvent];

			if (authCode == FAKE_AUTH_CODE)
			{
				UserEvent? userEvent = await tableStorageService.GetEntryIfExistsAsync(SteamId, SelectedEvent);	

				if (userEvent is null)
				{
					//TODO: Pop-up please enter code or something.
					ArgumentNullException.ThrowIfNull(userEvent);
				}

				await CacheOnChooseEvent(userEvent.AuthCode);
			}

			else
			{
				await tableStorageService.CreateUserEventIfNotExistsAsync(SteamId, SelectedEvent, authCode);

				await CacheOnChooseEvent(authCode);
			}

			return RedirectToPage("/PickEms/Stage", new
			{
				eventId = SelectedEvent,
				eventName,
				SteamId,
				stage = Stages.Stage1
			});
		}

		public async Task<IActionResult> OnPostDelete()
		{
			await tableStorageService.DeleteEntityIfExistsAsync(SteamId, SelectedEvent);

			memoryCache.Remove($"TOURNAMENT_{SelectedEvent}_USER_{SteamId}_AUTHCODE");
			memoryCache.Remove($"USER_{SteamId}_TOURNAMENT_{SelectedEvent}_PICKS");

			return RedirectToPage("/Profile/Overview");
		}

		public async Task<IActionResult?> OnGetAuthCode(string eventId)
		{
			var authCode = await tableStorageService.GetEntryIfExistsAsync(SteamId, eventId);

			if (authCode == null) return NotFound();

			return new JsonResult(new { authCode.AuthCode });
		}

		private async Task CacheOnChooseEvent(string authCode)
		{
			cachingService.CacheAuthCode(SelectedEvent, SteamId, authCode);

			await cachingService.RefreshUserPredictionsAsync(SteamId, SelectedEvent);

			await cachingService.CacheTeamsAsync(SteamId, SelectedEvent);

			memoryCache.Set($"TOURNAMENT_{SelectedEvent}_USER_{SteamId}_AUTHCODE", authCode);
		}
	}
}

