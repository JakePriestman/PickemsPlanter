using PickemsPlanter.Models.Event;
using PickemsPlanter.Models.PandaScore;
using PickemsPlanter.Models.Simulator;
using PickemsPlanter.Models.Steam;
using System.Text.RegularExpressions;

namespace PickemsPlanter.Services;

public interface IPandaScoreResultsService
{
	Task<List<SimulatorMatchResult>> GetCompletedMatchesAsync(string eventId, Stages stage);
	Task<List<SimulatorLiveMatch>> GetLiveMatchesAsync(string eventId, Stages stage);
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
				Round = round.Value,
				Score = ResolveScore(match, winner.Id, loser.Id)
			});
		}

		return [.. results.OrderBy(r => r.Round)];
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

			var teamALogo = ResolveLogoFileName(teams, opponents[0].Name);
			var teamBLogo = ResolveLogoFileName(teams, opponents[1].Name);

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

	private static string? ResolveLogoFileName(IReadOnlyCollection<Team> teams, string? pandaScoreTeamName)
	{
		if (pandaScoreTeamName is null)
			return null;

		var exactMatches = teams.Where(x => x.Name!.Equals(pandaScoreTeamName, StringComparison.CurrentCultureIgnoreCase)).ToList();

		if (exactMatches.Count == 1)
			return $"{exactMatches[0].Logo}.png";

		// PandaScore and Steam sometimes use slightly different variants of the same org's name
		// (eg. "Liquid" vs "Team Liquid", "BetBoom Team" vs "BetBoom") — fall back to a
		// normalized substring match, but only commit when it resolves to exactly one candidate.
		string normalizedPandaScoreName = Normalize(pandaScoreTeamName);

		var fuzzyMatches = teams
			.Where(x =>
			{
				string normalizedTeamName = Normalize(x.Name!);

				if (normalizedTeamName == normalizedPandaScoreName
					|| normalizedTeamName.Contains(normalizedPandaScoreName)
					|| normalizedPandaScoreName.Contains(normalizedTeamName))
					return true;

				// Some orgs are commonly referred to by an initialism of their full name
				// (eg. "NiP" for "Ninjas in Pyjamas") — the initials aren't a contiguous
				// substring of the space-stripped name, so the checks above miss it.
				return GetInitials(x.Name!) == normalizedPandaScoreName
					|| GetInitials(pandaScoreTeamName) == normalizedTeamName;
			})
			.ToList();

		return fuzzyMatches.Count == 1 ? $"{fuzzyMatches[0].Logo}.png" : null;
	}

	private static string? ResolveScore(PandaScoreMatch match, int winnerId, int loserId)
	{
		var winnerResult = match.Results.FirstOrDefault(r => r.TeamId == winnerId);
		var loserResult = match.Results.FirstOrDefault(r => r.TeamId == loserId);

		return winnerResult is not null && loserResult is not null
			? $"{winnerResult.Score}-{loserResult.Score}"
			: null;
	}

	private static string Normalize(string name) =>
		string.Concat(name.Where(char.IsLetterOrDigit)).ToLowerInvariant();

	private static string GetInitials(string name) =>
		string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(word => word[0])).ToLowerInvariant();

	private static int? ParseRound(string matchName)
	{
		var match = RoundNumberRegex().Match(matchName);

		return match.Success ? int.Parse(match.Groups[1].Value) : null;
	}

	[GeneratedRegex(@"^Round (\d+):", RegexOptions.IgnoreCase)]
	private static partial Regex RoundNumberRegex();
}
