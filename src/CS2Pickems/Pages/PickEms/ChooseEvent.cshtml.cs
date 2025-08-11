using CS2Pickems.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;
using System.Net;
using System.Security.Claims;

namespace CS2Pickems.Pages.PickEms
{
    public class ChooseEventModel(ITableStorageService tableStoreService, IUserPredictionsCachingService cachingService, IMemoryCache memoryCache) : PageModel
    {
		[BindProperty]
		public string SelectedEvent { get; set; } = string.Empty;

		private readonly ITableStorageService _tableStorageService = tableStoreService;

		private readonly IUserPredictionsCachingService _cachingService = cachingService;

		private readonly IMemoryCache _memoryCache = memoryCache;

		public List<SelectListItem> EventOptions { get; set; } = [
				new SelectListItem { Value = "24", Text = "BLAST.tv Austin 2025 CS2 Major Championship" }
			];

		public void OnGet()
        {

		}

		public async Task<IActionResult> OnPost()
		{
			var steamId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

			if (steamId is not null)
			{
				var tableEntity = await _tableStorageService.GetEntryIfExistsAsync(steamId, SelectedEvent);

				var eventName = EventOptions.First(x => x.Value == SelectedEvent).Text;

				var encodedName = WebUtility.UrlEncode(eventName);

				if (tableEntity is not null)
				{
					await _cachingService.RefreshUserPredictionsAsync(steamId, SelectedEvent, tableEntity.AuthCode);

					_memoryCache.Set($"TOURNAMENT_{SelectedEvent}_USER_{steamId}_AUTHCODE", tableEntity.AuthCode);

					return RedirectToPage("/PickEms/Stage1", new
					{
						eventId = SelectedEvent,
						eventName,
						steamId
					});
				}
		
				return RedirectToPage("/PickEms/AddAuthCode", new
				{
					eventId = SelectedEvent,
					eventName,
					steamId
				});
			}

			else
				return Redirect("/Login");
		}
    }
}
