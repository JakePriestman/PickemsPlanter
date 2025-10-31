using CS2Pickems.Models;
using CS2Pickems.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CS2Pickems.Pages.PickEms
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

		public async Task<JsonResult> OnGetImages()
        {
			try
            {
                return new JsonResult(await pickemsService.GetTeamsInStageAsync(Stage, EventId));
            }
            catch
            {
                throw;
            }
        }

        public async Task<JsonResult> OnGetPicks()
        {
			try
            {
                return new JsonResult(await pickemsService.GetStagePicksAsync(Stage, SteamId, EventId));
            }
            catch
            {
                throw;
            }
		}

        public async Task OnPostSendPicks(string droppedImagesData)
        {
			var authCode = cachingService.GetAuthCodeFromCache(EventId, SteamId);

			try
            {
                await pickemsService.PostStagePickemsAsync(Stage, droppedImagesData, SteamId, EventId, authCode);
            }
            catch
            {
                throw;
            }
        }
    }
}
