using CS2Pickems.APIs;
using CS2Pickems.Models;
using CS2Pickems.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using System.Threading.Tasks;

namespace CS2Pickems.Pages
{
    public class PickEmStage1Model(ISteamAPI steamAPI, IMemoryCache cache, IPickemsService pickemsService) : PageModel
    {
        private readonly ISteamAPI _steamAPI = steamAPI;
        private readonly Section _section = (Section) cache.Get("STAGE_1")!;
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
