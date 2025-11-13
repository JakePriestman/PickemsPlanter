using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using PickemsPlanter.Services;

namespace PickemsPlanter.Pages.PickEms
{

	public class PlayoffsModel(IPickemsService pickemsService, List<SelectListItem> eventOptions, IHttpContextAccessor httpContextAccessor, IUserPredictionsCachingService cachingService) : PageModel
	{
		[BindProperty(SupportsGet = true)]
		public required string EventId { get; init; }

		[BindProperty(SupportsGet = true)]
		public required string EventName { get; init; }

		[BindProperty(SupportsGet = true)]
		public required string SteamId { get; init; }

		public required string? PersonaName = httpContextAccessor?.HttpContext?.User.FindFirst("PersonaName")?.Value;

		public required string? Avatar = httpContextAccessor?.HttpContext?.User.FindFirst("Avatar")?.Value;

		[BindProperty(SupportsGet = true)]
		public string? SelectedEvent { get; init; }

		public List<SelectListItem> EventOptions { get; set; } = eventOptions;

		public async Task<JsonResult> OnGetPicksAllowed()
		{
			bool picksAllowed = await pickemsService.GetPlayoffsPicksAllowedAsync(EventId);
			return new JsonResult(picksAllowed);
		}

		public async Task<JsonResult> OnGetImages()
        {
			return new JsonResult(await pickemsService.GetTeamsInPlayoffsAsync(EventId));
        }

        public async Task<JsonResult> OnGetPicks()
        {
			return new JsonResult(await pickemsService.GetPlayoffPicksAsync(SteamId, EventId));
        }

		public async Task<JsonResult> OnGetResults()
		{
			return new JsonResult(await pickemsService.GetPlayoffResultsAsync(EventId));
		}

		public async Task<IActionResult> OnPostSendPicks(string droppedImagesData)
		{
			var authCode = cachingService.GetAuthCodeFromCache(EventId, SteamId);

			await pickemsService.PostPlayoffPickemsAsync(droppedImagesData, SteamId, EventId, authCode);

			return RedirectToPage("/PickEms/Playoffs", new
			{
				EventId,
				EventName = EventOptions.First(e => e.Value == EventId).Text,
				SteamId
			});
		}
	}
}
