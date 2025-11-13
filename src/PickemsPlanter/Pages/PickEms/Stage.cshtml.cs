using PickemsPlanter.Models;
using PickemsPlanter.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PickemsPlanter.Pages.PickEms
{
    public class StageModel(IPickemsService pickemsService, List<SelectListItem> eventOptions, IHttpContextAccessor httpContextAccessor, IUserPredictionsCachingService cachingService) : PageModel
    {
		[BindProperty(SupportsGet = true)]
		public required string EventId { get; init; }

		[BindProperty(SupportsGet = true)]
		public required string EventName { get; init; }

		[BindProperty(SupportsGet = true)]
		public required string SteamId { get; init; }

        [BindProperty(SupportsGet = true)]
        public required Stages Stage { get; init; }

        public required string? PersonaName = httpContextAccessor?.HttpContext?.User.FindFirst("PersonaName")?.Value;

        public required string? Avatar = httpContextAccessor?.HttpContext?.User.FindFirst("Avatar")?.Value;

		[BindProperty(SupportsGet = true)]
		public string? SelectedEvent { get; init; }

		public List<SelectListItem> EventOptions { get; set; } = eventOptions;

		public async Task<JsonResult> OnGetPicksAllowed()
		{
			bool picksAllowed = await pickemsService.GetStagePicksAllowedAsync(Stage, EventId);

			return new JsonResult(picksAllowed);
		}

		public async Task<JsonResult> OnGetImages()
        {
            return new JsonResult(await pickemsService.GetTeamsInStageAsync(Stage, EventId));
        }

        public async Task<JsonResult> OnGetPicks()
        {
            return new JsonResult(await pickemsService.GetStagePicksAsync(Stage, SteamId, EventId));
		}

        public async Task<JsonResult> OnGetResults()
        {
            return new JsonResult(await pickemsService.GetStageResultsAsync(Stage, EventId));
		}

        public async Task<IActionResult> OnPostSendPicks(string droppedImagesData)
        {
			var authCode = cachingService.GetAuthCodeFromCache(EventId, SteamId);

            await pickemsService.PostStagePickemsAsync(Stage, droppedImagesData, SteamId, EventId, authCode);

			return RedirectToPage("/PickEms/Stage", new
			{
				EventId,
				EventName = EventOptions.First(e => e.Value == EventId).Text,
				SteamId,
				stage = Stages.Stage1
			});
		}
    }
}
