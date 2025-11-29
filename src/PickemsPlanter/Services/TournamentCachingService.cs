using PickemsPlanter.APIs;
using PickemsPlanter.Models;
using PickemsPlanter.Models.Steam;
using Microsoft.Extensions.Caching.Memory;

namespace PickemsPlanter.Services
{
	public interface ITournamentCachingService
	{
		Task<Section> GetSectionAsync(string eventId, Stages stage);
		Task<IReadOnlyCollection<Team>> GetTournamentTeamsAsync(string eventId);
		Task<IReadOnlyCollection<Section>> GetPlayoffsAsync(string eventId);
		Task<Stages> GetFirstActiveStageOrDefaultAsync(string eventId);
	}

	public class TournamentCachingService(ISteamAPI steamAPI, IMemoryCache cache) : ITournamentCachingService
	{

		public async Task<Section> GetSectionAsync(string eventId, Stages stage)
		{
			string key = $"TOURNAMENT_{eventId}_{stage}";

			if (!cache.TryGetValue(key, out Section? section))
			{
				var layout = await steamAPI.GetTournamentLayoutAsync(eventId);
				section = layout.Result.Sections[(int)stage];
				cache.Set(key, section, TimeSpan.FromMinutes(10));
			}

			return section!;
		}

		public async Task<IReadOnlyCollection<Team>> GetTournamentTeamsAsync(string eventId)
		{
			string key = $"TOURNAMENT_{eventId}_TEAMS";

			if (!cache.TryGetValue(key, out IReadOnlyCollection<Team>? teams) || teams is null)
			{
				var layout = await steamAPI.GetTournamentLayoutAsync(eventId);
				teams = layout.Result.Teams;
				cache.Set(key, teams);
			}

			return teams;
		}

		public async Task<IReadOnlyCollection<Section>> GetPlayoffsAsync(string eventId)
		{
			string key = $"TOURNAMENT_{eventId}_{Stages.Playoffs}";

			if (!cache.TryGetValue(key, out IReadOnlyCollection<Section>? sections))
			{
				var layout = await steamAPI.GetTournamentLayoutAsync(eventId);

				sections = [.. layout.Result.Sections.Skip(3)];

				cache.Set(key, sections, TimeSpan.FromMinutes(10));
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
