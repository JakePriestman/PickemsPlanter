using PickemsPlanter.Models.Event;
using PickemsPlanter.Models.Simulator;

namespace PickemsPlanter.Services;

public interface IPandaScoreResultsService
{
	Task<List<SimulatorMatchResult>> GetCompletedMatchesAsync(string eventId, Stages stage);
	Task<List<SimulatorLiveMatch>> GetLiveMatchesAsync(string eventId, Stages stage);
}

public class PandaScoreResultsService(IPandaScoreResultsCachingService cachingService, ITournamentCachingService tournamentCachingService) : IPandaScoreResultsService
{
	public async Task<List<SimulatorMatchResult>> GetCompletedMatchesAsync(string eventId, Stages stage)
	{
		var matches = cachingService.GetCompletedMatches(eventId, stage);

		if (matches.Count == 0)
			return [];

		var teams = await tournamentCachingService.GetTournamentTeamsAsync(eventId);

		return PandaScoreMatchMapper.ToCompletedMatchResults(matches, teams);
	}

	public async Task<List<SimulatorLiveMatch>> GetLiveMatchesAsync(string eventId, Stages stage)
	{
		var matches = cachingService.GetLiveMatches(eventId, stage);

		if (matches.Count == 0)
			return [];

		var teams = await tournamentCachingService.GetTournamentTeamsAsync(eventId);

		var results = new List<SimulatorLiveMatch>();

		foreach (var match in matches)
		{
			var opponents = match.Opponents.Select(o => o.Opponent).ToList();

			if (opponents.Count != 2)
				continue;

			var teamALogo = PandaScoreMatchMapper.ResolveLogoFileName(teams, opponents[0].Name);
			var teamBLogo = PandaScoreMatchMapper.ResolveLogoFileName(teams, opponents[1].Name);

			// Unresolved team name — skip rather than guess, same as the completed-matches path.
			if (teamALogo is null || teamBLogo is null)
				continue;

			results.Add(new SimulatorLiveMatch
			{
				TeamA = teamALogo,
				TeamB = teamBLogo
			});
		}

		return results;
	}
}
