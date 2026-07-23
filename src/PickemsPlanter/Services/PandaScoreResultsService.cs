using PickemsPlanter.Models.Event;
using PickemsPlanter.Models.PandaScore;
using PickemsPlanter.Models.Simulator;
using PickemsPlanter.Models.Steam;
using System.Text.RegularExpressions;

namespace PickemsPlanter.Services;

public interface IPandaScoreResultsService
{
	Task<List<SimulatorMatchResult>> GetCompletedMatchesAsync(string eventId, Stages stage);
}

public partial class PandaScoreResultsService(IPandaScoreResultsCachingService cachingService, ITournamentCachingService tournamentCachingService) : IPandaScoreResultsService
{
	public async Task<List<SimulatorMatchResult>> GetCompletedMatchesAsync(string eventId, Stages stage)
	{
		var matches = cachingService.GetCompletedMatches(eventId, stage);

		if (matches.Count == 0)
			return [];

		var teams = await tournamentCachingService.GetTournamentTeamsAsync(eventId);

		var results = new List<SimulatorMatchResult>();

		foreach (var match in matches)
		{
			int? round = ParseRound(match.Name);

			if (round is null)
				continue;

			var opponents = match.Opponents.Select(o => o.Opponent).ToList();

			if (opponents.Count != 2)
				continue;

			var winner = opponents.FirstOrDefault(o => o.Id == match.WinnerId);
			var loser = opponents.FirstOrDefault(o => o.Id != match.WinnerId);

			if (winner is null || loser is null)
				continue;

			var winnerLogo = ResolveLogoFileName(teams, winner.Name);
			var loserLogo = ResolveLogoFileName(teams, loser.Name);

			// Unresolved team name — skip rather than guess, leave that matchup fully manual.
			if (winnerLogo is null || loserLogo is null)
				continue;

			results.Add(new SimulatorMatchResult
			{
				WinnerTeam = winnerLogo,
				LoserTeam = loserLogo,
				Round = round.Value
			});
		}

		return [.. results.OrderBy(r => r.Round)];
	}

	private static string? ResolveLogoFileName(IReadOnlyCollection<Team> teams, string? pandaScoreTeamName)
	{
		if (pandaScoreTeamName is null)
			return null;

		var logo = teams.FirstOrDefault(x => x.Name!.Equals(pandaScoreTeamName, StringComparison.CurrentCultureIgnoreCase))?.Logo;

		return logo is null ? null : $"{logo}.png";
	}

	private static int? ParseRound(string matchName)
	{
		var match = RoundNumberRegex().Match(matchName);

		return match.Success ? int.Parse(match.Groups[1].Value) : null;
	}

	[GeneratedRegex(@"^Round (\d+):", RegexOptions.IgnoreCase)]
	private static partial Regex RoundNumberRegex();
}
