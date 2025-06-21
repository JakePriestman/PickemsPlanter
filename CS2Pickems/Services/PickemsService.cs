using CS2Pickems.APIs;
using CS2Pickems.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace CS2Pickems.Services
{
	public interface IPickemsService
	{
		List<string> GetTeamsInStage(Section section);
		List<string> GetTeamsInPlayoffs(List<Section> playoffs);
		Task<List<string>> GetStagePicksAsync(Section section);
		Task<List<string>> GetPlayoffPicksAsync(List<Section> playoffs);
		Task PostStagePickemsAsync(Section section, string droppedImagesData);
		Task PostPlayoffPickemsAsync(List<Section> playoffs, string droppedImagesData);

	}

	public class PickemsService(ISteamAPI steamAPI, IMemoryCache cache) : IPickemsService
	{
		private readonly IMemoryCache _cache = cache;
		private readonly ISteamAPI _steamAPI = steamAPI;
		private readonly List<Team> _teams = (List<Team>)cache.Get("TEAMS")!;

		public List<string> GetTeamsInStage(Section section)
		{
			var imageUrls = new List<string>();

			foreach (var team in section.Groups.First().Teams)
			{
				var logo = _teams.First(x => x.PickId == team.PickId).Logo;

				imageUrls.Add($"/Images/{logo}.png");
			}

			return imageUrls;
		}

		public List<string> GetTeamsInPlayoffs(List<Section> playoffs)
		{
			var imageUrls = new List<string>();

			var section = playoffs.First();

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
			return imageUrls;
		}

		public async Task<List<string>> GetStagePicksAsync(Section section)
		{
			var imageUrls = new List<string>();

			var picks = await _steamAPI.GetUserPredictionsAsync();

			var picksInGroup = picks.Result.Picks.Where(x => x.GroupId == section.Groups.First().GroupId);

			if (picksInGroup is null) return null!;

			foreach (var pick in picksInGroup)
			{
				var logo = _teams.First(x => x.PickId == pick.Pick).Logo;

				imageUrls.Add($"/Images/{logo}.png");
			}
			return imageUrls;
		}

		public async Task<List<string>> GetPlayoffPicksAsync(List<Section> playoffs)
		{
			List<string>? imageUrls = [];

			var picks = await _steamAPI.GetUserPredictionsAsync();

			foreach (var section in playoffs)
			{
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
			}
			return imageUrls;
		}

		public async Task PostStagePickemsAsync(Section section, string droppedImagesData)
		{
			if (!section.Groups.First().PicksAllowed) return;

			List<string> imageNames = [.. JsonSerializer.Deserialize<List<string>>(droppedImagesData)!.Select(x => x.Replace(".png", ""))];

			await _steamAPI.PostUserPredictionsAsync(imageNames, _teams, section.SectionId, section.Groups.First().GroupId);
		}

		public async Task PostPlayoffPickemsAsync(List<Section> playoffs, string droppedImagesData)
		{
			throw new NotImplementedException();
		}

	}
}
