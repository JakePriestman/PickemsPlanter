using CS2Pickems.APIs;
using CS2Pickems.Models.Steam;
using CS2Pickems.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;

namespace CS2Pickems.Pages.PickEms
{

	public class PlayoffsModel(IPickemsService pickemsService, List<SelectListItem> eventOptions, IHttpContextAccessor httpContextAccessor) : PageModel
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

		private string AuthCode { get; set; } = string.Empty;

		private readonly IPickemsService _pickemsService = pickemsService;

		public async Task<JsonResult> OnGetImages()
        {
			AuthCode = _pickemsService.GetAuthCodeFromCache(EventId, SteamId);

			try
			{
				return new JsonResult(await _pickemsService.GetTeamsInPlayoffsAsync(EventId));
			}
			catch 
			{
				throw;
			}
        }

        public async Task<JsonResult> OnGetPicks()
        {
			AuthCode = _pickemsService.GetAuthCodeFromCache(EventId, SteamId);

			try
			{
				return new JsonResult(await _pickemsService.GetPlayoffPicksAsync(SteamId, EventId, AuthCode));
			}
			catch
			{
				throw;
			}
        }

        public async Task OnPostSendPicks(string droppedImagesData)
		{
			AuthCode = _pickemsService.GetAuthCodeFromCache(EventId, SteamId);

			try
			{
				await _pickemsService.PostPlayoffPickemsAsync(droppedImagesData, SteamId, EventId, AuthCode);
			}

			catch
			{
				throw;
			}
		}
	}
}
