using CS2Pickems.APIs;
using CS2Pickems.Models.Steam;
using Microsoft.Extensions.Caching.Memory;

namespace CS2Pickems.Services
{
	public interface IUserPredictionsCachingService
	{

		Task<UserPredictions> CacheUserPredictionsAsync(string steamId, string eventId, string authCode);
		Task RefreshUserPredictionsAsync(string steamId, string eventId, string authCode);
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
				GetResult<UserPredictions> userPredictions = await _steamAPI.GetUserPredictionsAsync(steamId, eventId);
				_cache.Set(key, userPredictions.Result, TimeSpan.FromMinutes(2));
			}

			return picks!;
		}

		public async Task RefreshUserPredictionsAsync(string steamId, string eventId, string authCode)
		{
			string key = $"USER_{steamId}_TOURNAMENT_{eventId}_PICKS";

			GetResult<UserPredictions> userPredictions = await _steamAPI.GetUserPredictionsAsync(steamId, eventId);
			_cache.Set(key, userPredictions.Result, TimeSpan.FromMinutes(2));
		}
	}
}
