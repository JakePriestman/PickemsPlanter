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

	public class PickemsService(IUserPredictionsCachingService cachingService, ISteamAPI steamAPI, ITournamentCachingService tournamentCachingService) : IPickemsService
	{
		private const string IMAGE_LOCATION = "/Images/teams";

		public async Task<bool> GetStagePicksAllowedAsync(Stages stage, string eventId)
		{
			Section section = await tournamentCachingService.GetSectionAsync(eventId, stage);

			return section.Groups.First().PicksAllowed;
		}

		public async Task<List<string>> GetTeamsInStageAsync(Stages stage, string eventId)
		{
			IReadOnlyCollection<Team> teams = await tournamentCachingService.GetTournamentTeamsAsync(eventId);

			Section section = await tournamentCachingService.GetSectionAsync(eventId, stage);

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
			IReadOnlyCollection<Team> teams = await tournamentCachingService.GetTournamentTeamsAsync(eventId);

			Section section = await tournamentCachingService.GetSectionAsync(eventId, stage);

			List<string> imageUrls = [];

			List<int> pickIds = [];

			int index = 1;

			foreach (var picks in section.Groups.First().Picks)
			{
				if (picks.PickIds.Any())
					pickIds.AddRange(picks.PickIds);
				else
					pickIds.Add(index*-10);

				index++;
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
			IReadOnlyCollection<Team> teams = await cachingService.GetUserTeamsFromCacheAsync(steamId, eventId);

			Section section = await tournamentCachingService.GetSectionAsync(eventId, stage);

			if (!section.Groups.First().PicksAllowed) return;

			List<string> imageNames = [.. JsonSerializer.Deserialize<List<string>>(droppedImagesData)!.Select(x => x.Replace(".png", ""))];

			await steamAPI.PostUserPredictionsAsync(imageNames, teams, section.SectionId, section.Groups.First().GroupId, steamId, eventId, authCode);

			await cachingService.RefreshUserPredictionsAsync(steamId, eventId);
		}
		 
		public async Task<List<string>> GetStagePicksAsync(Stages stage, string steamId, string eventId)
		{
			List<string> imageUrls = [];

			IReadOnlyCollection<Team> teams = await cachingService.GetUserTeamsFromCacheAsync(steamId, eventId);

			Dictionary<int, Team> teamLookup = teams.ToDictionary(t => t.PickId);

			Section section = await tournamentCachingService.GetSectionAsync(eventId, stage);

			if (section?.Groups == null || !section.Groups.Any()) return imageUrls;

			UserPredictions picks = await cachingService.CacheUserPredictionsAsync(steamId, eventId);

			if (picks?.Picks is null) return imageUrls;

			int groupId = section.Groups.First().GroupId;

			var picksInGroup = picks.Picks.Where(x => x.GroupId == groupId).ToList();

			if (picksInGroup.Count == 0) return imageUrls;

			foreach (var pick in picksInGroup)
			{
				if (teamLookup.TryGetValue(pick.Pick, out var team) && team.Logo is not null)
					imageUrls.Add($"{IMAGE_LOCATION}/{team.Logo}.png");
				else
					imageUrls.Add($"{IMAGE_LOCATION}/unknown.png");
			}
			return imageUrls;
		}

		public async Task<bool> GetPlayoffsPicksAllowedAsync(string eventId)
		{
			IReadOnlyCollection<Section> playoffs = await tournamentCachingService.GetPlayoffsAsync(eventId);

			return playoffs.Any(x => x.Groups.Any(x => x.PicksAllowed));
		}

		public async Task<List<string>> GetTeamsInPlayoffsAsync(string eventId)
		{
			IReadOnlyCollection<Team> teams = await tournamentCachingService.GetTournamentTeamsAsync(eventId);

			IReadOnlyCollection<Section> playoffs = await tournamentCachingService.GetPlayoffsAsync(eventId);

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
			IReadOnlyCollection<Team> teams = await cachingService.GetUserTeamsFromCacheAsync(steamId, eventId);

			IReadOnlyCollection<Section> playoffs = await tournamentCachingService.GetPlayoffsAsync(eventId);

			List<string>? imageUrls = [];

			var picks = await cachingService.CacheUserPredictionsAsync(steamId, eventId);

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
			IReadOnlyCollection<Team> teams = await tournamentCachingService.GetTournamentTeamsAsync(eventId);

			IReadOnlyCollection<Section> playoffs = await tournamentCachingService.GetPlayoffsAsync(eventId);

			List<string>? imageUrls = [];

			List<int> pickIds = [];

			foreach (var section in playoffs)
			{
				foreach (var group in section.Groups)
				{
					IEnumerable<int> ids = group.Picks.SelectMany(x => x.PickIds);

					if (ids.Any())
						pickIds.AddRange(group.Picks.SelectMany(x => x.PickIds));
					else
						pickIds.Add(0);
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
			IReadOnlyCollection<Team> teams = await cachingService.GetUserTeamsFromCacheAsync(steamId, eventId);

			IReadOnlyCollection<Section> playoffs = await tournamentCachingService.GetPlayoffsAsync(eventId);

			List<string> imageNames = [.. JsonSerializer.Deserialize<List<string>>(droppedImagesData)!.Select(x => x.Replace(".png", ""))];

			await steamAPI.PostPlayoffPredictionsAsync(imageNames, teams, playoffs, steamId, eventId, authCode);

			await cachingService.RefreshUserPredictionsAsync(steamId, eventId);
		}
	}
}
