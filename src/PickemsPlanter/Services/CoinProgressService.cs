using PickemsPlanter.Models.CoinProgress;
using PickemsPlanter.Models.Event;

namespace PickemsPlanter.Services;

public interface ICoinProgressService
{
	Task<CoinProgressResult> GetCoinProgressAsync(string steamId, string eventId);
}

// Rules per blast.tv/major/pickems, assumed the same for every event: 11 total challenges,
// 0-3 completed = Bronze, 4-7 = Silver, 8-10 = Gold, 11 = Diamond. Reuses IPickemsService's
// existing picks/results data (the same source already powering the "Show results" checkmark
// UI) rather than reading the coin item itself, which isn't reliably exposed via the Web API.
public class CoinProgressService(IPickemsService pickemsService) : ICoinProgressService
{
	private const int TotalChallenges = 11;

	public async Task<CoinProgressResult> GetCoinProgressAsync(string steamId, string eventId)
	{
		List<ChallengeProgress> challenges =
		[
			// Granted by buying the event's Viewer Pass — not independently verifiable from
			// app data, but submitting real Steam-backed predictions already requires pass
			// ownership, so any user with predictions on record has necessarily activated it.
			new() { Name = "Coin Activation", Completed = true },
		];

		challenges.AddRange(await GetStageChallengesAsync(Stages.Stage1, "Stage 1", steamId, eventId));
		challenges.AddRange(await GetStageChallengesAsync(Stages.Stage2, "Stage 2", steamId, eventId));
		challenges.AddRange(await GetStageChallengesAsync(Stages.Stage3, "Stage 3", steamId, eventId));
		challenges.AddRange(await GetPlayoffChallengesAsync(steamId, eventId));

		int completed = challenges.Count(c => c.Completed);

		return new CoinProgressResult
		{
			Tier = ResolveTier(completed),
			CompletedChallenges = completed,
			TotalChallenges = TotalChallenges,
			Challenges = challenges
		};
	}

	private async Task<List<ChallengeProgress>> GetStageChallengesAsync(Stages stage, string label, string steamId, string eventId)
	{
		List<string> picks = await pickemsService.GetStagePicksAsync(stage, steamId, eventId);
		List<string> results = await pickemsService.GetStageResultsAsync(stage, eventId);
		bool picksAllowed = await pickemsService.GetStagePicksAllowedAsync(stage, eventId);

		bool participation = HasParticipated(picks, picksAllowed, expectedCount: 10);

		// Categories per stage.js's dropzone layout: pick0-1 = 3-0, pick2-7 = 3-1/3-2 combined
		// (the app doesn't distinguish a 3-1 finish from a 3-2 one for scoring), pick8-9 = 0-3.
		int correct = CountCorrectInCategory(picks, results, start: 0, length: 2)
			+ CountCorrectInCategory(picks, results, start: 2, length: 6)
			+ CountCorrectInCategory(picks, results, start: 8, length: 2);

		return
		[
			new() { Name = $"{label} Participation", Completed = participation },
			new() { Name = $"{label} Accuracy", Completed = correct >= 5 }
		];
	}

	private async Task<List<ChallengeProgress>> GetPlayoffChallengesAsync(string steamId, string eventId)
	{
		List<string> picks = await pickemsService.GetPlayoffPicksAsync(steamId, eventId);
		List<string> results = await pickemsService.GetPlayoffResultsAsync(eventId);
		bool picksAllowed = await pickemsService.GetPlayoffsPicksAllowedAsync(eventId);

		bool participation = HasParticipated(picks, picksAllowed, expectedCount: 7);

		// Bracket order per the app's own playoffs slicing convention (see toggleCheckmark in
		// stylingFunctions.js): picks 0-3 are the quarter-finals predictions, 4-5 semi-finals, 6 the champion.
		int quarterFinalsCorrect = CountCorrectInCategory(picks, results, start: 0, length: 4);
		int semiFinalsCorrect = CountCorrectInCategory(picks, results, start: 4, length: 2);
		bool grandFinalCorrect = IsCorrect(picks.ElementAtOrDefault(6), results.ElementAtOrDefault(6));

		return
		[
			new() { Name = "Playoffs Participation", Completed = participation },
			new() { Name = "Quarter-Finals", Completed = quarterFinalsCorrect >= 2 },
			new() { Name = "Semi-Finals", Completed = semiFinalsCorrect >= 1 },
			new() { Name = "Grand Final", Completed = grandFinalCorrect }
		];
	}

	// Only counts once picks are locked — a still-open stage/playoffs round can't be scored
	// either way yet, since the user could still fill in (or change) their remaining picks.
	private static bool HasParticipated(List<string> picks, bool picksAllowed, int expectedCount) =>
		!picksAllowed && picks.Count == expectedCount && picks.All(pick => !pick.Contains("unknown"));

	// Set membership within the category, not positional equality — predicting *which* teams
	// land in a given outcome (eg. "these two teams go 3-0") is what's scored, not which exact
	// slot within that category each one was dropped into. Steam's official-results grouping
	// (GetStageResultsAsync/GetPlayoffResultsAsync) has no reason to order a category's teams
	// the same way the user happened to place their own predictions, so a naive positional zip
	// undercounts real matches whenever the order differs — this mirrors toggleCheckmark's own
	// category slicing + .includes() membership check in stylingFunctions.js exactly.
	private static int CountCorrectInCategory(IReadOnlyList<string> picks, IReadOnlyList<string> results, int start, int length)
	{
		var resultSet = results.Skip(start).Take(length)
			.Where(result => !result.Contains("unknown"))
			.ToHashSet();

		return picks.Skip(start).Take(length)
			.Count(pick => !pick.Contains("unknown") && resultSet.Contains(pick));
	}

	private static bool IsCorrect(string? pick, string? result) =>
		pick is not null && result is not null && pick == result && !pick.Contains("unknown");

	private static CoinTier ResolveTier(int completed) => completed switch
	{
		TotalChallenges => CoinTier.Diamond,
		>= 8 => CoinTier.Gold,
		>= 4 => CoinTier.Silver,
		_ => CoinTier.Bronze
	};
}
