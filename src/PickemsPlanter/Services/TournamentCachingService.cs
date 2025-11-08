using PickemsPlanter.APIs;
using PickemsPlanter.Models;
using PickemsPlanter.Models.Steam;
using Microsoft.Extensions.Caching.Memory;

namespace PickemsPlanter.Services
{
	public interface ITournamentCachingService
	{
		Task<Section> GetSectionAsync(string eventId, Stages stage);
		Task<IReadOnlyCollection<Section>> GetPlayoffsAsync(string eventId);
	}

	public class TournamentCachingService(ISteamAPI steamAPI, IMemoryCache cache) : ITournamentCachingService
	{
		private readonly IMemoryCache _cache = cache;
		private readonly ISteamAPI _steamAPI = steamAPI;

		public async Task<Section> GetSectionAsync(string eventId, Stages stage)
		{
			string key = $"TOURNAMENT_{eventId}_{stage}";

			if (!_cache.TryGetValue(key, out Section? sectionFromCache))
			{
				var layout = await _steamAPI.GetTournamentLayoutAsync(eventId);
				var section = layout.Result.Sections[(int)stage];
				_cache.Set(key, section, TimeSpan.FromMinutes(10));
			}

			return sectionFromCache!;
		}

		public async Task<IReadOnlyCollection<Section>> GetPlayoffsAsync(string eventId)
		{
			string key = $"TOURNAMENT_{eventId}_{Stages.Playoffs}";

			if (!_cache.TryGetValue(key, out IReadOnlyCollection<Section>? sectionFromCache))
			{
				var layout = await _steamAPI.GetTournamentLayoutAsync(eventId);
				layout.Result.Sections.Remove(layout.Result.Sections[0]);
				layout.Result.Sections.Remove(layout.Result.Sections[0]);
				layout.Result.Sections.Remove(layout.Result.Sections[0]);

				var sections = layout.Result.Sections[0];
				_cache.Set(key, sections, TimeSpan.FromMinutes(10));
			}

			return sectionFromCache!;
		}
	}
}
