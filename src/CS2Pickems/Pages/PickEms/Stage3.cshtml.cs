using CS2Pickems.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;

namespace CS2Pickems.Pages.PickEms
{
	public class Stage3Model(IPickemsService pickemsService) : PageModel
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
				return new JsonResult(await _pickemsService.GetTeamsInStageAsync(Models.Stages.Stage3, EventId));
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
				return new JsonResult(await _pickemsService.GetStagePicksAsync(Models.Stages.Stage3, SteamId, EventId, AuthCode));
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
				await _pickemsService.PostStagePickemsAsync(Models.Stages.Stage3, droppedImagesData, SteamId, EventId, AuthCode);
			}
			catch
			{
				throw;
			}
		}
	}
}
