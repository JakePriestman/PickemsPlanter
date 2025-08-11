using CS2Pickems.APIs;
using CS2Pickems.Models;
using CS2Pickems.Models.Steam;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace CS2Pickems.Services
{
	public interface IPickemsService
	{
		string GetAuthCodeFromCache(string eventId, string steamId);
		Task<List<string>> GetTeamsInStageAsync(Stages stage, string eventId);
		Task<List<string>> GetTeamsInPlayoffsAsync(string eventId);
		Task<List<string>> GetStagePicksAsync(Stages stage, string steamId, string eventId, string authCode);
		Task<List<string>> GetPlayoffPicksAsync(string steamId, string eventId, string authCode);
		Task PostStagePickemsAsync(Stages stage, string droppedImagesData, string steamId, string eventId, string authCode);
		Task PostPlayoffPickemsAsync(string droppedImagesData, string AuthCode);

	}

	public class PickemsService(IUserPredictionsCachingService cachingService, IMemoryCache cache, ISteamAPI steamAPI, ITournamentCachingService tournamentCachingService) : IPickemsService
	{
		private readonly IMemoryCache _cache = cache;
		private readonly IUserPredictionsCachingService _cachingService = cachingService;
		private readonly ISteamAPI _steamAPI = steamAPI;
		private readonly ITournamentCachingService _tournamentCachingService = tournamentCachingService;

		public string GetAuthCodeFromCache(string eventId, string steamId)
		{
			//TODO - Should this be here and should this be responsible for setting the cache as well?
			return _cache.Get<string>($"TOURNAMENT_{eventId}_USER_{steamId}_AUTHCODE")!;
		}

		public async Task<List<string>> GetTeamsInStageAsync(Stages stage, string eventId)
		{
			List<Team> teams = (List<Team>)_cache.Get($"TOURNAMENT_{eventId}_TEAMS")!;

			Section section = await _tournamentCachingService.GetSectionAsync(eventId, stage);

			var imageUrls = new List<string>();

			foreach (var team in section.Groups.First().Teams)
			{
				var logo = teams.First(x => x.PickId == team.PickId).Logo;

				imageUrls.Add($"/Images/{logo}.png");
			}

			return imageUrls;
		}

		public async Task<List<string>> GetTeamsInPlayoffsAsync(string eventId)
		{
			List<Team> teams = (List<Team>)_cache.Get($"TOURNAMENT_{eventId}_TEAMS")!;

			IReadOnlyCollection<Section> playoffs = await _tournamentCachingService.GetPlayoffsAsync(eventId);

			var imageUrls = new List<string>();

			var section = playoffs.First();

			foreach (var group in section.Groups)
			{
				foreach (var team in group.Teams)
				{
					var logo = teams.FirstOrDefault(x => x.PickId == team.PickId)?.Logo;

					if (logo is null)
						imageUrls.Add($"/Images/unknown.png");
					else
						imageUrls.Add($"/Images/{logo}.png");
				}
			}
			return imageUrls;
		}

		public async Task<List<string>> GetStagePicksAsync(Stages stage, string steamId, string eventId, string authCode)
		{
			List<Team> teams = (List<Team>)_cache.Get($"TOURNAMENT_{eventId}_TEAMS")!;

			Section section = await _tournamentCachingService.GetSectionAsync(eventId, stage);

			var imageUrls = new List<string>();

			var picks = await _cachingService.CacheUserPredictionsAsync(steamId, eventId, authCode);

			var picksInGroup = picks.Picks.Where(x => x.GroupId == section.Groups.First().GroupId);

			if (picksInGroup is null) return null!;

			foreach (var pick in picksInGroup)
			{
				var logo = teams.First(x => x.PickId == pick.Pick).Logo;

				imageUrls.Add($"/Images/{logo}.png");
			}
			return imageUrls;
		}

		public async Task<List<string>> GetPlayoffPicksAsync(string steamId, string eventId, string authCode)
		{
			IReadOnlyCollection<Team> teams = (IReadOnlyCollection<Team>)_cache.Get($"TOURNAMENT_{eventId}_TEAMS")!;

			IReadOnlyCollection<Section> playoffs = await _tournamentCachingService.GetPlayoffsAsync(eventId);

			List<string>? imageUrls = [];

			var picks = await _cachingService.CacheUserPredictionsAsync(steamId, eventId, authCode);

			foreach (var section in playoffs)
			{
				foreach (var group in section.Groups)
				{
					var picksInGroup = picks.Picks.Where(x => x.GroupId == group.GroupId);

					if (!picksInGroup.Any()) continue;

					foreach (var pick in picksInGroup)
					{
						var logo = teams.First(x => x.PickId == pick.Pick).Logo;

						imageUrls.Add($"/Images/{logo}.png");
					}
				}
			}
			return imageUrls;
		}

		public async Task PostStagePickemsAsync(Stages stage, string droppedImagesData, string steamId, string eventId, string authCode)
		{
			List<Team> teams = (List<Team>)_cache.Get($"TOURNAMENT_{eventId}_TEAMS")!;

			Section section = await _tournamentCachingService.GetSectionAsync(eventId, stage);

			if (!section.Groups.First().PicksAllowed) return;

			List<string> imageNames = [.. JsonSerializer.Deserialize<List<string>>(droppedImagesData)!.Select(x => x.Replace(".png", ""))];

			await _steamAPI.PostUserPredictionsAsync(imageNames, teams, section.SectionId, section.Groups.First().GroupId, steamId, eventId);

			await _cachingService.RefreshUserPredictionsAsync(steamId, eventId, authCode);
		}

		public async Task PostPlayoffPickemsAsync(string droppedImagesData, string AuthCode)
		{
			throw new NotImplementedException();
		}

	}
}
