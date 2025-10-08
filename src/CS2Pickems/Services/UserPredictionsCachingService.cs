using CS2Pickems.APIs;
using CS2Pickems.Models;
using CS2Pickems.Models.Steam;
using Microsoft.Extensions.Caching.Memory;

namespace CS2Pickems.Services
{
	public interface IUserPredictionsCachingService
	{
		Task<UserPredictions> CacheUserPredictionsAsync(string steamId, string eventId, string authCode);
		Task RefreshUserPredictionsAsync(string steamId, string eventId, string authCode);
		Task CacheTeamsAsync(string steamId, string eventId, string authCode);
	} 

	public class UserPredictionsCachingService(IMemoryCache cache, ISteamAPI steamAPI) : IUserPredictionsCachingService
	{
		private readonly IMemoryCache _cache = cache;
		private readonly ISteamAPI _steamAPI = steamAPI;

		public async Task<UserPredictions> CacheUserPredictionsAsync(string steamId, string eventId, string authCode)
		{
			string key = $"USER_{steamId}_TOURNAMENT_{eventId}_PICKS";

			if (!_cache.TryGetValue(key, out UserPredictions? picks))
			{
				GetResult<UserPredictions> userPredictions = await _steamAPI.GetUserPredictionsAsync(steamId, eventId, authCode);
				_cache.Set(key, userPredictions.Result, TimeSpan.FromMinutes(2));

				return userPredictions.Result;
			}

			return picks!;
		}

		public async Task RefreshUserPredictionsAsync(string steamId, string eventId, string authCode)
		{
			string key = $"USER_{steamId}_TOURNAMENT_{eventId}_PICKS";

			GetResult<UserPredictions> userPredictions = await _steamAPI.GetUserPredictionsAsync(steamId, eventId, authCode);
			_cache.Set(key, userPredictions.Result, TimeSpan.FromMinutes(2));
		}

		public async Task CacheTeamsAsync(string steamId, string eventId, string authCode)
		{
			string key = $"USER_{steamId}_TOURNAMENT_{eventId}_TEAMS";

			IEnumerable<Team>? teams = (IEnumerable<Team>?)_cache.Get($"TOURNAMENT_{eventId}_TEAMS");

			if (teams is null)
			{
				var layout = await _steamAPI.GetTournamentLayoutAsync(eventId);

				teams = layout.Result.Teams;
			}

			GetResult<TournamentItems> items =  await _steamAPI.GetTournamentItemsAsync(steamId, eventId, authCode);

			foreach (var team in teams)
			{
				var teamFromitems = items.Result.Items.First(x => x.TeamId == team.PickId);

				team.TeamId = teamFromitems.TeamId;
				team.ItemId = teamFromitems.ItemId;
				team.Type = teamFromitems.Type;
			}

			_cache.Set(key, teams);
		}
	}
}
