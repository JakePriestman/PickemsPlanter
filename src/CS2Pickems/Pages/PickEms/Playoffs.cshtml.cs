using CS2Pickems.APIs;
using CS2Pickems.Models.Steam;
using CS2Pickems.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;

namespace CS2Pickems.Pages.PickEms
{

	public class PlayoffsModel(IPickemsService pickemsService) : PageModel
	{
		[BindProperty(SupportsGet = true)]
		public required string EventId { get; init; }

		[BindProperty(SupportsGet = true)]
		public required string EventName { get; init; }

		[BindProperty(SupportsGet = true)]
		public required string SteamId { get; init; }

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
				await _pickemsService.PostPlayoffPickemsAsync(droppedImagesData, AuthCode);
			}

			catch
			{
				throw;
			}
		}
	}
}
