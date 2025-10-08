using CS2Pickems.Models;
using CS2Pickems.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;
using System.Net;
using System.Security.Claims;

namespace CS2Pickems.Pages.Profile
{
	public class OverviewModel(ITableStorageService tableStoreService, IUserPredictionsCachingService cachingService, IMemoryCache memoryCache, List<SelectListItem> eventOptions, IHttpContextAccessor httpContextAccessor) : PageModel
	{
		[BindProperty]
		public string SelectedEvent { get; set; } = string.Empty;

		private readonly ITableStorageService _tableStorageService = tableStoreService;

		private readonly IUserPredictionsCachingService _cachingService = cachingService;

		private readonly IMemoryCache _memoryCache = memoryCache;

		public required string? PersonaName = httpContextAccessor?.HttpContext?.User.FindFirst("PersonaName")?.Value;

		public required string? Avatar = httpContextAccessor?.HttpContext?.User.FindFirst("Avatar")?.Value;

		public required string SteamId = httpContextAccessor?.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;

		public List<SelectListItem> EventOptions { get; set; } = eventOptions;

		[BindProperty]
		public Dictionary<string, string> AuthCodes { get; set; } = eventOptions.ToDictionary(key => key.Value, value => string.Empty);

		public async Task OnGetAsync()
		{
			foreach (var key in AuthCodes.Keys)
			{
				var tableEntity = await _tableStorageService.GetEntryIfExistsAsync(SteamId, key);

				if (tableEntity is not null)
					AuthCodes[key] = tableEntity.AuthCode;
			}
		}

		public async Task<IActionResult> OnPostChooseEvent()
		{
			//Set cache and everything here & add code to storage if not exists
			var authCode = AuthCodes[SelectedEvent];

			var eventName = EventOptions.First(x => x.Value == SelectedEvent).Text;

			var encodedName = WebUtility.UrlEncode(eventName);

			await _cachingService.RefreshUserPredictionsAsync(SteamId, SelectedEvent, authCode);

			await _cachingService.CacheTeamsAsync(SteamId, SelectedEvent, authCode);

			_memoryCache.Set($"TOURNAMENT_{SelectedEvent}_USER_{SteamId}_AUTHCODE", authCode);

			return RedirectToPage("/PickEms/Stage", new
			{
				eventId = SelectedEvent,
				eventName,
				SteamId,
				stage = Stages.Stage1
			});
		}

		public async Task OnPostDelete()
		{
			await _tableStorageService.DeleteEntityIfExistsAsync(SteamId, SelectedEvent);

			_memoryCache.Remove($"TOURNAMENT_{SelectedEvent}_USER_{SteamId}_AUTHCODE");
			_memoryCache.Remove($"USER_{SteamId}_TOURNAMENT_{SelectedEvent}_PICKS");
		}
	}
}

