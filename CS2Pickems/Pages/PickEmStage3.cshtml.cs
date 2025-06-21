using CS2Pickems.APIs;
using CS2Pickems.Models;
using CS2Pickems.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace CS2Pickems.Pages
{
	public class PickEmStage3Model(ISteamAPI steamAPI, IMemoryCache cache, IPickemsService pickemsService) : PageModel
	{
		private readonly IMemoryCache _cache = cache;
		private readonly ISteamAPI _steamAPI = steamAPI;
		private readonly List<Team> _teams = (List<Team>)cache.Get("TEAMS")!;
		private readonly Section _section = (Section)cache.Get("STAGE_3")!;
		private readonly IPickemsService _pickemsService = pickemsService;

		public async Task<JsonResult> OnGetImages()
		{
			try
			{
				return new JsonResult(_pickemsService.GetTeamsInStage(_section));
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
				return new JsonResult(await _pickemsService.GetStagePicksAsync(_section));
			}
			catch
			{
				throw;
			}
		}

		public async Task OnPostSendPicks(string droppedImagesData)
		{
			try
			{
				await _pickemsService.PostStagePickemsAsync(_section, droppedImagesData);
			}
			catch
			{
				throw;
			}
		}
	}
}
