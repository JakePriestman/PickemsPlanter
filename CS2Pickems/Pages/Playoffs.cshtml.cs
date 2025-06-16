using CS2Pickems.APIs;
using CS2Pickems.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;

namespace CS2Pickems.Pages
{

	public class PlayoffsModel(ISteamAPI steamAPI, IMemoryCache cache) : PageModel
	{
		private readonly IMemoryCache _cache = cache;
		private readonly ISteamAPI _steamAPI = steamAPI;
		private readonly List<Team> _teams = (List<Team>)cache.Get("TEAMS")!;
		private readonly List<Section> _sections = (List<Section>)cache.Get("PLAYOFFS")!;

		public JsonResult OnGetImages()
        {
			var imageUrls = new List<string>();

			var section = _sections.First();

            foreach (var group in section.Groups)
            {
                foreach (var team in group.Teams)
                {
                    var logo = _teams.FirstOrDefault(x => x.PickId == team.PickId)?.Logo;

                    if (logo is null)
                        imageUrls.Add($"/Images/unknown.png");
                    else
                        imageUrls.Add($"/Images/{logo}.png");
                }
            }

            return new JsonResult(imageUrls);
        }

        public async Task<JsonResult> OnGetPicks()
        {
            List<string>? imageUrls = [];

            var picks = await _steamAPI.GetUserPredictionsAsync();

            foreach (var section in _sections)
            {
                imageUrls.AddRange(GetPicks(section, picks));
            }

            return new JsonResult(imageUrls);
        }
        private List<string> GetPicks(Section section, GetResponse<UserPredictions> picks)
        {
            List<string> imageUrls = [];

            foreach (var group in section.Groups)
            {
                var picksInGroup = picks.Result.Picks.Where(x => x.GroupId == group.GroupId);

                if (!picksInGroup.Any()) continue;

                foreach (var pick in picksInGroup)
                {
                    var logo = _teams.First(x => x.PickId == pick.Pick).Logo;

                    imageUrls.Add($"/Images/{logo}.png");
                }
            }

            return imageUrls;
        }

        public async Task OnPostSendPicks(string droppedImagesData)
		{
			//if (!_section.Groups.First().PicksAllowed) return;

			//List<string> imageNames = [.. JsonSerializer.Deserialize<List<string>>(droppedImagesData).Select(x => x.Replace(".png", ""))];

			//await _steamAPI.PostUserPredictionsAsync(imageNames, _teams, _section.SectionId, _section.Groups.First().GroupId);
		}
	}
}
