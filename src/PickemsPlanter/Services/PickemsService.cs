using Microsoft.Extensions.Caching.Memory;
using PickemsPlanter.APIs;
using PickemsPlanter.Models;
using PickemsPlanter.Models.Steam;
using System.Text.Json;

namespace PickemsPlanter.Services
{
	public interface IPickemsService
	{
		Task<bool> GetStagePicksAllowedAsync(Stages stage, string eventId);
		Task<List<string>> GetTeamsInStageAsync(Stages stage, string eventId);
		Task<List<string>> GetStagePicksAsync(Stages stage, string steamId, string eventId);
		Task<List<string>> GetStageResultsAsync(Stages stage, string eventId);
		Task PostStagePickemsAsync(Stages stage, string droppedImagesData, string steamId, string eventId, string authCode);

		Task<bool> GetPlayoffsPicksAllowedAsync(string eventId);
		Task<List<string>> GetTeamsInPlayoffsAsync(string eventId);
		Task<List<string>> GetPlayoffPicksAsync(string steamId, string eventId);
		Task<List<string>> GetPlayoffResultsAsync(string eventId);
		Task PostPlayoffPickemsAsync(string droppedImagesData, string steamId, string eventId, string authCode);
	}

	public class PickemsService(IUserPredictionsCachingService cachingService, IMemoryCache cache, ISteamAPI steamAPI, ITournamentCachingService tournamentCachingService) : IPickemsService
	{
		private readonly IMemoryCache _cache = cache;
		private readonly IUserPredictionsCachingService _cachingService = cachingService;
		private readonly ISteamAPI _steamAPI = steamAPI;
		private readonly ITournamentCachingService _tournamentCachingService = tournamentCachingService;
		private const string IMAGE_LOCATION = "/Images/teams";

		public async Task<bool> GetStagePicksAllowedAsync(Stages stage, string eventId)
		{
			Section section = await _tournamentCachingService.GetSectionAsync(eventId, stage);

			return section.Groups.First().PicksAllowed;
		}

		public async Task<List<string>> GetTeamsInStageAsync(Stages stage, string eventId)
		{
			List<Team> teams = (List<Team>)_cache.Get($"TOURNAMENT_{eventId}_TEAMS")!;

			Section section = await _tournamentCachingService.GetSectionAsync(eventId, stage);

			var imageUrls = new List<string>();

			foreach (var team in section.Groups.First().Teams)
			{
				var logo = teams.FirstOrDefault(x => x.PickId == team.PickId)?.Logo;

				if (logo is null)
					imageUrls.Add($"{IMAGE_LOCATION}/unknown.png");
				else
					imageUrls.Add($"{IMAGE_LOCATION}/{logo}.png");
			}

			return imageUrls;
		}

		public async Task<List<string>> GetStageResultsAsync(Stages stage, string eventId)
		{
			List<Team> teams = (List<Team>)_cache.Get($"TOURNAMENT_{eventId}_TEAMS")!;

			Section section = await _tournamentCachingService.GetSectionAsync(eventId, stage);

			List<string> imageUrls = [];

			List<int> pickIds = [];

			foreach (var picks in section.Groups.First().Picks)
			{
				pickIds.AddRange(picks.PickIds);
			}

			foreach (var pickId in pickIds.Distinct())
			{
				var logo = teams.FirstOrDefault(x => x.PickId == pickId)?.Logo;

				if (logo is null)
					imageUrls.Add($"{IMAGE_LOCATION}/unknown.png");
				else
					imageUrls.Add($"{IMAGE_LOCATION}/{logo}.png");
			}

			return imageUrls;
		}

		public async Task PostStagePickemsAsync(Stages stage, string droppedImagesData, string steamId, string eventId, string authCode)
		{
			List<Team> teams = (List<Team>)_cache.Get($"USER_{steamId}_TOURNAMENT_{eventId}_TEAMS")!;

			Section section = await _tournamentCachingService.GetSectionAsync(eventId, stage);

			if (!section.Groups.First().PicksAllowed) return;

			List<string> imageNames = [.. JsonSerializer.Deserialize<List<string>>(droppedImagesData)!.Select(x => x.Replace(".png", ""))];

			await _steamAPI.PostUserPredictionsAsync(imageNames, teams, section.SectionId, section.Groups.First().GroupId, steamId, eventId, authCode);

			await _cachingService.RefreshUserPredictionsAsync(steamId, eventId);
		}

		public async Task<List<string>> GetStagePicksAsync(Stages stage, string steamId, string eventId)
		{
			List<Team> teams = (List<Team>)_cache.Get($"USER_{steamId}_TOURNAMENT_{eventId}_TEAMS")!;

			Section section = await _tournamentCachingService.GetSectionAsync(eventId, stage);

			var imageUrls = new List<string>();

			var picks = await _cachingService.CacheUserPredictionsAsync(steamId, eventId);

			var picksInGroup = picks.Picks.Where(x => x.GroupId == section.Groups.First().GroupId);

			if (picksInGroup is null) return null!;

			foreach (var pick in picksInGroup)
			{
				var logo = teams.First(x => x.PickId == pick.Pick).Logo;

				imageUrls.Add($"{IMAGE_LOCATION}/{logo}.png");
			}
			return imageUrls;
		}

		public async Task<bool> GetPlayoffsPicksAllowedAsync(string eventId)
		{
			IReadOnlyCollection<Section> playoffs = await _tournamentCachingService.GetPlayoffsAsync(eventId);

			return playoffs.First().Groups.First().PicksAllowed;
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
						imageUrls.Add($"{IMAGE_LOCATION}/unknown.png");
					else
						imageUrls.Add($"{IMAGE_LOCATION}/{logo}.png");
				}
			}
			return imageUrls;
		}

		public async Task<List<string>> GetPlayoffPicksAsync(string steamId, string eventId)
		{
			IReadOnlyCollection<Team> teams = (IReadOnlyCollection<Team>)_cache.Get($"USER_{steamId}_TOURNAMENT_{eventId}_TEAMS")!;

			IReadOnlyCollection<Section> playoffs = await _tournamentCachingService.GetPlayoffsAsync(eventId);

			List<string>? imageUrls = [];

			var picks = await _cachingService.CacheUserPredictionsAsync(steamId, eventId);

			foreach (var section in playoffs)
			{
				foreach (var group in section.Groups)
				{
					var picksInGroup = picks.Picks.Where(x => x.GroupId == group.GroupId);

					if (!picksInGroup.Any()) continue;

					foreach (var pick in picksInGroup)
					{
						var logo = teams.First(x => x.PickId == pick.Pick).Logo;

						imageUrls.Add($"{IMAGE_LOCATION}/{logo}.png");
					}
				}
			}
			return imageUrls;
		}

		public async Task<List<string>> GetPlayoffResultsAsync(string eventId)
		{
			List<Team> teams = (List<Team>)_cache.Get($"TOURNAMENT_{eventId}_TEAMS")!;

			IReadOnlyCollection<Section> playoffs = await _tournamentCachingService.GetPlayoffsAsync(eventId);

			List<string>? imageUrls = [];

			List<int> pickIds = [];

			foreach (var section in playoffs)
			{
				foreach (var group in section.Groups)
				{
					pickIds.AddRange(group.Picks.SelectMany(x => x.PickIds));
				}
			}

			foreach (var pickId in pickIds)
			{
				var logo = teams.FirstOrDefault(x => x.PickId == pickId)?.Logo;

				if (logo is null)
					imageUrls.Add($"{IMAGE_LOCATION}/unknown.png");
				else
					imageUrls.Add($"{IMAGE_LOCATION}/{logo}.png");
			}

			return imageUrls;
		}
		
		public async Task PostPlayoffPickemsAsync(string droppedImagesData, string steamId, string eventId, string authCode)
		{
			List<Team> teams = (List<Team>)_cache.Get($"USER_{steamId}_TOURNAMENT_{eventId}_TEAMS")!;
			
			IReadOnlyCollection<Section> playoffs = await _tournamentCachingService.GetPlayoffsAsync(eventId);

			List<string> imageNames = [.. JsonSerializer.Deserialize<List<string>>(droppedImagesData)!.Select(x => x.Replace(".png", ""))];

			await _steamAPI.PostPlayoffPredictionsAsync(imageNames, teams, playoffs, steamId, eventId, authCode);

			await _cachingService.RefreshUserPredictionsAsync(steamId, eventId);
		}
	}
}
