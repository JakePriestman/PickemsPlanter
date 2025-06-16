using CS2Pickems.APIs;
using CS2Pickems.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace CS2Pickems.Pages
{
	public class PickEmStage2Model(ISteamAPI steamAPI, IMemoryCache cache) : PageModel
	{
		private readonly IMemoryCache _cache = cache;
		private readonly ISteamAPI _steamAPI = steamAPI;
		private readonly List<Team> _teams = (List<Team>)cache.Get("TEAMS")!;
		private readonly Section _section = (Section)cache.Get("STAGE_2")!;

		public JsonResult OnGetImages()
		{
			var imageUrls = new List<string>();

			foreach (var team in _section.Groups.First().Teams)
			{
				var logo = _teams.FirstOrDefault(x => x.PickId == team.PickId)?.Logo;

				if (logo is null)
					imageUrls.Add($"/Images/unknown.png");
				else
					imageUrls.Add($"/Images/{logo}.png");
			}

			return new JsonResult(imageUrls);
		}

		public async Task<JsonResult> OnGetPicks()
		{
			var imageUrls = new List<string>();

			var picks = await _steamAPI.GetUserPredictionsAsync();

			var picksInGroup = picks.Result.Picks.Where(x => x.GroupId == _section.Groups.First().GroupId);

			if (picksInGroup is null) return null!;

			foreach (var pick in picksInGroup)
			{
				var logo = _teams.First(x => x.PickId == pick.Pick).Logo;

				imageUrls.Add($"/Images/{logo}.png");
			}

			return new JsonResult(imageUrls);
		}

		public async Task OnPostSendPicks(string droppedImagesData)
		{
			if (!_section.Groups.First().PicksAllowed) return;

			List<string> imageNames = [.. JsonSerializer.Deserialize<List<string>>(droppedImagesData)!.Select(x => x.Replace(".png", ""))];

			await _steamAPI.PostUserPredictionsAsync(imageNames, _teams, _section.SectionId, _section.Groups.First().GroupId);
		}
	}
}