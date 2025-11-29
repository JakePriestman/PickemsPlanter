using Microsoft.Extensions.Caching.Memory;
using PickemsPlanter.APIs;
using PickemsPlanter.Models.Steam;
using System;

namespace PickemsPlanter.Services
{
	public interface IUserPredictionsCachingService
	{
		void CacheAuthCode(string eventId, string steamId, string authCode);
		string GetAuthCodeFromCache(string eventId, string steamId);
		void RemoveAuthCodeFromCache(string eventId, string steamId);
		Task<UserPredictions> CacheUserPredictionsAsync(string steamId, string eventId);
		Task<UserPredictions> RefreshUserPredictionsAsync(string steamId, string eventId);
		Task<IReadOnlyCollection<Team>> GetUserTeamsFromCacheAsync(string steamId, string eventId);
		Task<IReadOnlyCollection<Team>> CacheUserTeamsAsync(string steamId, string eventId);
		void EmptyUserCache(string steamId, string eventId);
	} 

	public class UserPredictionsCachingService(IMemoryCache cache, ISteamAPI steamAPI, ITournamentCachingService tournamentCachingService) : IUserPredictionsCachingService
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
			cache.Set(key, userPredictions.Result, TimeSpan.FromMinutes(10));

			return userPredictions.Result;
		}

		public async Task<IReadOnlyCollection<Team>> GetUserTeamsFromCacheAsync(string steamId, string eventId)
		{
			string key = $"USER_{steamId}_TOURNAMENT_{eventId}_TEAMS";

			if (!cache.TryGetValue(key, out IReadOnlyCollection<Team>? teams) && teams is not null)
			{
				teams = await CacheUserTeamsAsync(steamId, eventId);
			}

			return teams!;
		}

		public async Task<IReadOnlyCollection<Team>> CacheUserTeamsAsync(string steamId, string eventId)
		{
			string key = $"USER_{steamId}_TOURNAMENT_{eventId}_TEAMS";

			if (cache.TryGetValue(key, out IReadOnlyCollection<Team>? userTeams) &&  userTeams is not null)
				return userTeams;

			string authCode = GetAuthCodeFromCache(eventId, steamId);

			var teams = await tournamentCachingService.GetTournamentTeamsAsync(eventId);

			GetResult<TournamentItems> items =  await steamAPI.GetTournamentItemsAsync(steamId, eventId, authCode);

			foreach (var team in teams)
			{
				var teamFromItems = items.Result.Items.First(x => x.TeamId == team.PickId);

				team.TeamId = teamFromItems.TeamId;
				team.ItemId = teamFromItems.ItemId;
				team.Type = teamFromItems.Type;
			}

			cache.Set(key, teams, TimeSpan.FromMinutes(30));

			return teams;
		}

		public void EmptyUserCache(string steamId, string eventId)
		{
			cache.Remove($"USER_{steamId}_TOURNAMENT_{eventId}_TEAMS");
			cache.Remove($"USER_{steamId}_TOURNAMENT_{eventId}_PICKS");
			RemoveAuthCodeFromCache(eventId, steamId);
		}
	}
}
