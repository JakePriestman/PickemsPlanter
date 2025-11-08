using PickemsPlanter.APIs;
using PickemsPlanter.Models.Steam;
using Microsoft.Extensions.Caching.Memory;

namespace PickemsPlanter.Services
{
	public interface IUserPredictionsCachingService
	{
		void CacheAuthCode(string eventId, string steamId, string authCode);
		string GetAuthCodeFromCache(string eventId, string steamId);
		void RemoveAuthCodeFromCache(string eventId, string steamId);
		Task<UserPredictions> CacheUserPredictionsAsync(string steamId, string eventId);
		Task<UserPredictions> RefreshUserPredictionsAsync(string steamId, string eventId);
		Task CacheTeamsAsync(string steamId, string eventId);
		void EmptyUserCache(string steamId, string eventId);
	} 

	public class UserPredictionsCachingService(IMemoryCache cache, ISteamAPI steamAPI) : IUserPredictionsCachingService
	{
		public void CacheAuthCode(string eventId, string steamId, string authCode)
		{
			cache.Set($"TOURNAMENT_{eventId}_USER_{steamId}_AUTHCODE", authCode);
		}
		public string GetAuthCodeFromCache(string eventId, string steamId)
		{
			return cache.Get<string>($"TOURNAMENT_{eventId}_USER_{steamId}_AUTHCODE")!;
		}

		public void RemoveAuthCodeFromCache(string eventId, string steamId)
		{
			cache.Remove($"TOURNAMENT_{eventId}_USER_{steamId}_AUTHCODE");
		}

		public async Task<UserPredictions> CacheUserPredictionsAsync(string steamId, string eventId)
		{
			string key = $"USER_{steamId}_TOURNAMENT_{eventId}_PICKS";

			if (!cache.TryGetValue(key, out UserPredictions? picks))
			{
				return await RefreshUserPredictionsAsync(steamId, eventId);
			}

			return picks!;
		}

		public async Task<UserPredictions> RefreshUserPredictionsAsync(string steamId, string eventId)
		{
			string key = $"USER_{steamId}_TOURNAMENT_{eventId}_PICKS";

			string authCode = GetAuthCodeFromCache(eventId, steamId);

			GetResult<UserPredictions> userPredictions = await steamAPI.GetUserPredictionsAsync(steamId, eventId, authCode);
			cache.Set(key, userPredictions.Result, TimeSpan.FromMinutes(2));

			return userPredictions.Result;
		}

		public async Task CacheTeamsAsync(string steamId, string eventId)
		{
			string key = $"USER_{steamId}_TOURNAMENT_{eventId}_TEAMS";

			if (cache.TryGetValue(key, out IEnumerable<Team>? userTeams))
				return;

			string authCode = GetAuthCodeFromCache(eventId, steamId);

			IEnumerable<Team>? teams = (IEnumerable<Team>?)cache.Get($"TOURNAMENT_{eventId}_TEAMS");

			if (teams is null)
			{
				var layout = await steamAPI.GetTournamentLayoutAsync(eventId);

				teams = layout.Result.Teams;
			}

			GetResult<TournamentItems> items =  await steamAPI.GetTournamentItemsAsync(steamId, eventId, authCode);

			foreach (var team in teams)
			{
				var teamFromitems = items.Result.Items.First(x => x.TeamId == team.PickId);

				team.TeamId = teamFromitems.TeamId;
				team.ItemId = teamFromitems.ItemId;
				team.Type = teamFromitems.Type;
			}

			cache.Set(key, teams);
		}

		public void EmptyUserCache(string steamId, string eventId)
		{
			cache.Remove($"USER_{steamId}_TOURNAMENT_{eventId}_TEAMS");
			cache.Remove($"USER_{steamId}_TOURNAMENT_{eventId}_PICKS");
			RemoveAuthCodeFromCache(eventId, steamId);
		}
	}
}
