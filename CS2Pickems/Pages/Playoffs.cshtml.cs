using CS2Pickems.APIs;
using CS2Pickems.Models;
using CS2Pickems.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;

namespace CS2Pickems.Pages
{

	public class PlayoffsModel(ISteamAPI steamAPI, IMemoryCache cache, IPickemsService pickemsService) : PageModel
	{
		private readonly ISteamAPI _steamAPI = steamAPI;
		private readonly List<Section> _sections = (List<Section>)cache.Get("PLAYOFFS")!;
		private readonly IPickemsService _pickemsService = pickemsService;

		public JsonResult OnGetImages()
        {
			try
			{
				return new JsonResult(_pickemsService.GetTeamsInPlayoffs(_sections));
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
				return new JsonResult(await _pickemsService.GetPlayoffPicksAsync(_sections));
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
				await _pickemsService.PostPlayoffPickemsAsync(_sections, droppedImagesData);
			}

			catch
			{
				throw;
			}
		}
	}
}
