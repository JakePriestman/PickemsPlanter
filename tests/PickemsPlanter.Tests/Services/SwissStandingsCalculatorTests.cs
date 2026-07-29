using PickemsPlanter.Models.Simulator;
using Xunit;

namespace PickemsPlanter.Services;

public class SwissStandingsCalculatorTests
{
	private readonly SwissStandingsCalculator _calculator = new();

	// A fully resolved, hand-verified 16-team Swiss stage. T1-T8 finish 3-x (advancing),
	// T9-T16 finish x-3 (eliminated). Buchholz for each advancing team is each opponent's
	// final (Wins - Losses) summed, matching the standard Buchholz/"difficulty score"
	// definition simulator.js approximates client-side (see SwissStandingsCalculator's
	// class comment for why this port uses true-final rather than round-frozen values).
	private static List<SimulatorMatchResult> FullyResolvedStage() =>
	[
		Match("t1.png", "t9.png"), Match("t1.png", "t10.png"), Match("t1.png", "t11.png"),
		Match("t2.png", "t12.png"), Match("t2.png", "t13.png"), Match("t2.png", "t14.png"),
		Match("t3.png", "t15.png"), Match("t3.png", "t16.png"), Match("t3.png", "t9.png"),
		Match("t4.png", "t10.png"), Match("t4.png", "t11.png"), Match("t4.png", "t12.png"),
		Match("t5.png", "t13.png"), Match("t5.png", "t14.png"), Match("t5.png", "t15.png"),
		Match("t6.png", "t16.png"), Match("t6.png", "t9.png"), Match("t6.png", "t10.png"),
		Match("t7.png", "t11.png"), Match("t7.png", "t12.png"), Match("t7.png", "t13.png"),
		Match("t8.png", "t14.png"), Match("t8.png", "t15.png"), Match("t8.png", "t16.png"),

		// Upsets: each of these gives an eliminated team one of its (0-2) wins, and gives
		// the advancing team one of its (0-2) losses.
		Match("t9.png", "t5.png"), Match("t9.png", "t6.png"),
		Match("t10.png", "t5.png"), Match("t10.png", "t7.png"),
		Match("t11.png", "t6.png"), Match("t11.png", "t7.png"),
		Match("t12.png", "t3.png"),
		Match("t13.png", "t4.png"),
		Match("t14.png", "t8.png")
	];

	private static Dictionary<string, int> SeedsByTeam() =>
		Enumerable.Range(1, 16).ToDictionary(n => $"t{n}.png", n => n);

	private static SimulatorMatchResult Match(string winner, string loser) => new()
	{
		WinnerTeam = winner,
		LoserTeam = loser,
		Round = 1,
		IsBestOfThree = false
	};

	[Fact]
	public void TryCalculateFinalStandings_ReturnsFalse_WhenNoResults()
	{
		// Act
		bool success = _calculator.TryCalculateFinalStandings([], SeedsByTeam(), out var standings);

		// Assert
		Assert.False(success);
		Assert.Empty(standings);
	}

	[Fact]
	public void TryCalculateFinalStandings_ReturnsFalse_WhenFewerThan16TeamsAppear()
	{
		// Arrange — only a handful of the 16 teams have played any match.
		List<SimulatorMatchResult> results = [Match("t1.png", "t9.png"), Match("t1.png", "t10.png")];

		// Act
		bool success = _calculator.TryCalculateFinalStandings(results, SeedsByTeam(), out var standings);

		// Assert
		Assert.False(success);
	}

	[Fact]
	public void TryCalculateFinalStandings_ReturnsFalse_WhenATeamHasNotReachedATerminalRecord()
	{
		// Arrange — same as the fully resolved stage, but t8 is missing one of its wins
		// (t14 beating t8 is still present), leaving t8 at 2-1 instead of a terminal 3-x/x-3.
		var results = FullyResolvedStage();
		results.RemoveAll(r => r.WinnerTeam == "t8.png" && r.LoserTeam == "t14.png");

		// Act
		bool success = _calculator.TryCalculateFinalStandings(results, SeedsByTeam(), out var standings);

		// Assert
		Assert.False(success);
	}

	[Fact]
	public void TryCalculateFinalStandings_ReturnsFalse_WhenATeamHasNoInitialSeed()
	{
		// Arrange
		var seeds = SeedsByTeam();
		seeds.Remove("t1.png");

		// Act
		bool success = _calculator.TryCalculateFinalStandings(FullyResolvedStage(), seeds, out var standings);

		// Assert
		Assert.False(success);
	}

	[Fact]
	public void TryCalculateFinalStandings_OrdersAdvancingTeams_ByRecordThenBuchholzThenSeed()
	{
		// Act
		bool success = _calculator.TryCalculateFinalStandings(FullyResolvedStage(), SeedsByTeam(), out var standings);

		// Assert
		Assert.True(success);
		Assert.Equal(16, standings.Count);

		var advancing = standings.Where(s => s.Wins == 3).ToList();
		Assert.Equal(8, advancing.Count);

		// t1,t2: 3-0, Buchholz -3 and -6 respectively — record ties, Buchholz decides.
		// t4,t3,t8: 3-1, Buchholz -6/-9/-10 — Buchholz outranks seed (t4 has the worse seed
		// of the three but the best Buchholz among them).
		// t6,t7,t5: 3-2, Buchholz -7/-7/-9 — t6/t7 tie on Buchholz, seed (6 < 7) breaks the tie.
		string[] expectedOrder = ["t1.png", "t2.png", "t4.png", "t3.png", "t8.png", "t6.png", "t7.png", "t5.png"];
		Assert.Equal(expectedOrder, advancing.Select(s => s.Team));

		var eliminated = standings.Where(s => s.Wins < 3).Select(s => s.Team).ToHashSet();
		Assert.Equal(Enumerable.Range(9, 8).Select(n => $"t{n}.png").ToHashSet(), eliminated);
	}
}
