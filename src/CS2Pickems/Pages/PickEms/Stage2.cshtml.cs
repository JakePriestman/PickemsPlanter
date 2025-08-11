using CS2Pickems.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;

namespace CS2Pickems.Pages.PickEms
{
	public class Stage2Model(IPickemsService pickemsService) : PageModel
	{
		[BindProperty(SupportsGet = true)]
		public required string EventId { get; init; }

		[BindProperty(SupportsGet = true)]
		public required string EventName { get; init; }

		[BindProperty(SupportsGet = true)]
		public required string SteamId { get; init; }

		private readonly IPickemsService _pickemsService = pickemsService;

		private string AuthCode { get; set; } = string.Empty;

		public async Task<JsonResult> OnGetImages()
		{
			AuthCode = _pickemsService.GetAuthCodeFromCache(EventId, SteamId);

			try
			{
				return new JsonResult(await _pickemsService.GetTeamsInStageAsync(Models.Stages.Stage2, EventId));
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
				return new JsonResult(await _pickemsService.GetStagePicksAsync(Models.Stages.Stage2, SteamId, EventId, AuthCode));
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
				await _pickemsService.PostStagePickemsAsync(Models.Stages.Stage2, droppedImagesData, SteamId, EventId, AuthCode);
			}
			catch
			{
				throw;
			}
		}
	}
}