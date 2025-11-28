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
		Task<Stages> GetFirstActiveStageOrDefaultAsync(string eventId);
	}

	public class TournamentCachingService(ISteamAPI steamAPI, IMemoryCache cache) : ITournamentCachingService
	{
		private readonly IMemoryCache _cache = cache;
		private readonly ISteamAPI _steamAPI = steamAPI;

		public async Task<Section> GetSectionAsync(string eventId, Stages stage)
		{
			string key = $"TOURNAMENT_{eventId}_{stage}";

			if (!_cache.TryGetValue(key, out Section? section))
			{
				var layout = await _steamAPI.GetTournamentLayoutAsync(eventId);
				section = layout.Result.Sections[(int)stage];
				_cache.Set(key, section, TimeSpan.FromMinutes(10));
			}

			return section!;
		}

		public async Task<IReadOnlyCollection<Section>> GetPlayoffsAsync(string eventId)
		{
			string key = $"TOURNAMENT_{eventId}_{Stages.Playoffs}";

			if (!_cache.TryGetValue(key, out IReadOnlyCollection<Section>? sections))
			{
				var layout = await _steamAPI.GetTournamentLayoutAsync(eventId);

				sections = [.. layout.Result.Sections.Skip(3)];

				_cache.Set(key, sections, TimeSpan.FromMinutes(10));
			}

			return sections!;
		}

		public async Task<Stages> GetFirstActiveStageOrDefaultAsync(string eventId)
		{
			for (int i = 0; i < 4; i++)
			{
				if (i != 3)
				{
					var stage = (Stages)i;
					var section = await GetSectionAsync(eventId, stage);

					if (section.Groups.Any(x => x.PicksAllowed))
						return stage;

					continue;
				}

				var playoffs = await GetPlayoffsAsync(eventId);

				if (playoffs.Any(x => x.Groups.Any(x => x.PicksAllowed)))
					return Stages.Playoffs;
			}

			return Stages.Stage1;
		}
	}
}
