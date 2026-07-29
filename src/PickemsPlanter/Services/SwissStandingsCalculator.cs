using PickemsPlanter.Models.Simulator;

namespace PickemsPlanter.Services;

// One team's final standing in a resolved 16-team Swiss stage. Team is a logo filename
// (eg. "spirit.png"), matching SimulatorMatchResult's team identifiers.
public record SwissStanding(string Team, int Wins, int Losses, int Buchholz, int InitialSeed);

public interface ISwissStandingsCalculator
{
	bool TryCalculateFinalStandings(
		IReadOnlyCollection<SimulatorMatchResult> results,
		IReadOnlyDictionary<string, int> initialSeedByTeam,
		out IReadOnlyList<SwissStanding> standings);
}

// Server-side port of the DOM-based Buchholz/standings logic in wwwroot/js/Simulator/simulator.js
// (calculateNewBuchholzScores/getBracketId/getTeamsPreviousOpponents/orderSwissGroup), operating
// on a stage's full completed-match list instead of live DOM state.
//
// Deliberate deviation from a literal port: the client only ever has a partial, incrementally
// updated DOM, so it computes each team's Buchholz once per round and freezes it — an opponent
// locked in earlier (eg. 3-0 after round 2) never has their contribution refreshed with data
// from rounds that finish later for other teams. Here we always have the complete, final result
// set up front, so Buchholz is computed once, using every opponent's true final (Wins - Losses)
// — the standard Buchholz/"difficulty score" definition, and strictly more correct than
// replaying the client's staleness artifact.
public class SwissStandingsCalculator : ISwissStandingsCalculator
{
	public bool TryCalculateFinalStandings(
		IReadOnlyCollection<SimulatorMatchResult> results,
		IReadOnlyDictionary<string, int> initialSeedByTeam,
		out IReadOnlyList<SwissStanding> standings)
	{
		standings = [];

		if (results.Count == 0)
			return false;

		Dictionary<string, (int Wins, int Losses)> records = [];
		Dictionary<string, List<string>> opponentsByTeam = [];

		foreach (var result in results)
		{
			RecordOpponent(opponentsByTeam, result.WinnerTeam, result.LoserTeam);
			RecordOpponent(opponentsByTeam, result.LoserTeam, result.WinnerTeam);

			var (winnerWins, winnerLosses) = records.GetValueOrDefault(result.WinnerTeam);
			records[result.WinnerTeam] = (winnerWins + 1, winnerLosses);

			var (loserWins, loserLosses) = records.GetValueOrDefault(result.LoserTeam);
			records[result.LoserTeam] = (loserWins, loserLosses + 1);
		}

		// A resolved 16-team Swiss stage always ends with every team at a terminal 3-x/x-3
		// record — if that's not the case yet, the stage isn't actually done.
		if (records.Count != 16 || records.Values.Any(r => r.Wins != 3 && r.Losses != 3))
			return false;

		List<SwissStanding> calculated = [];

		foreach (var (team, record) in records)
		{
			// Can't tiebreak a team we don't have an initial seed for — bail rather than
			// guess an order (this points at a name-resolution gap between data sources).
			if (!initialSeedByTeam.TryGetValue(team, out int initialSeed))
				return false;

			int buchholz = opponentsByTeam[team].Sum(opponent =>
			{
				var opponentRecord = records[opponent];
				return opponentRecord.Wins - opponentRecord.Losses;
			});

			calculated.Add(new SwissStanding(team, record.Wins, record.Losses, buchholz, initialSeed));
		}

		// Wins is 3 for every advancing team, so it alone can't separate 3-0 from 3-1 from
		// 3-2 — fewer losses is a strictly better finish and must outrank Buchholz, which only
		// tiebreaks within an otherwise-equal record.
		standings = [.. calculated
			.OrderByDescending(s => s.Wins)
			.ThenBy(s => s.Losses)
			.ThenByDescending(s => s.Buchholz)
			.ThenBy(s => s.InitialSeed)];

		return true;
	}

	private static void RecordOpponent(Dictionary<string, List<string>> opponentsByTeam, string team, string opponent)
	{
		if (!opponentsByTeam.TryGetValue(team, out var opponents))
		{
			opponents = [];
			opponentsByTeam[team] = opponents;
		}

		opponents.Add(opponent);
	}
}
